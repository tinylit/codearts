using CodeArts.Db.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeArts.Db
{
    /// <summary>
    /// SqlServer矫正设置。
    /// </summary>
    public class SqlServerCorrectSettings : ISQLCorrectSettings
    {
        private static readonly Regex PatternWithAs = new Regex(@"\bwith[\x20\t\r\n\f]+[^\x20\t\r\n\f]+[\x20\t\r\n\f]+as[\x20\t\r\n\f]*\(.+?\)([\x20\t\r\n\f]*,[\x20\t\r\n\f]*[^\x20\t\r\n\f]+[\x20\t\r\n\f]+as[\x20\t\r\n\f]*\(.+?\))*[\x20\t\r\n\f]*(?=select|insert|update|delete[\x20\t\r\n\f]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex PatternSelect = new Regex(@"\bselect(([\x20\t\r\n\f]+distinct)+)?[\x20\t\r\n\f]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PatternAnyField = new Regex(@"\*[\x20\t\r\n\f]*$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        private static readonly Regex PatternSingleAsColumn = new Regex(@"(?<name>(\w+|\[\w+\]))[\x20\t\r\n\f]*$", RegexOptions.Compiled | RegexOptions.RightToLeft);

        /// <summary>
        /// 字符串截取。 SUBSTRING。
        /// </summary>
        public string Substring => "SUBSTRING";

        /// <summary>
        /// 字符串索引。 CHARINDEX。
        /// </summary>
        public string IndexOf => "CHARINDEX";

        /// <summary>
        /// 字符串长度。 LEN。
        /// </summary>
        public string Length => "LEN";

        /// <summary>
        /// 索引位置对调。 true。
        /// </summary>
        public bool IndexOfSwapPlaces => true;

        /// <summary>
        /// SqlServer。
        /// </summary>
        public DatabaseEngine Engine => DatabaseEngine.SqlServer;

        private ICollection<IFormatter> formatters;
        /// <summary>
        /// 格式化集合。
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER
        public ICollection<IFormatter> Formatters => formatters ??= new List<IFormatter>();
#else
        public ICollection<IFormatter> Formatters => formatters ?? (formatters = new List<IFormatter>());
#endif

        /// <summary>
        /// 字段。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public string Name(string name) => string.Concat("[", name, "]");

        /// <summary>
        /// 参数名称。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public string ParamterName(string name) => string.Concat("@", name);

        private static bool IsWhitespace(char c) => c == '\x20' || c == '\t' || c == '\r' || c == '\n' || c == '\f';

        private static readonly char[] FromChars = new char[] { 'f', 'r', 'o', 'm' };

        private static bool IsFrom(string sql, int startIndex)
        {
            for (int i = 0; i < 4; i++)
            {
                if (FromChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static readonly char[] DistinctChars = new char[] { 'd', 'i', 's', 't', 'i', 'n', 'c', 't' };

        private static bool IsDistinct(string sql, int startIndex)
        {
            for (int i = 0; i < 8; i++)
            {
                if (DistinctChars[i].Equals(char.ToLower(sql[startIndex + i])))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private class RangeMatch
        {
            public int Index { get; set; }

            public int Length { get; set; }
        }

        private static Tuple<int, int> AnalysisFields(string sql, out List<RangeMatch> matches)
        {
            int i = 0;
            int length = sql.Length;

            matches = new List<RangeMatch>();

            for (; i < length; i++) //? 空白符处理。
            {
                char c = sql[i];

                if (IsWhitespace(c))
                {
                    continue;
                }

                break;
            }

            bool flag = true;
            bool distinctFlag = true;
            bool quotesFlag = false;

            //? 字段初始。
            int fieldStart = 0;

            int startIndex = 0;

            int letterStart = 0;
            int bracketLeft = 0;
            int bracketRight = 0;

            for (i += 6; i < length; i++) //? 跳过第一个关键字。
            {
                char c = sql[i];

                if (c == '\\') //? 转义。
                {
                    i++;

                    continue;
                }

                if (c == '\'')
                {
                    if (quotesFlag) //? 右引号。
                    {
                        quotesFlag = false;

                        continue;
                    }

                    quotesFlag = true;
                }

                if (quotesFlag) //? 占位符。
                {
                    continue;
                }

                if (bracketLeft == bracketRight && c == ',') //? 字段。
                {
                    matches.Add(new RangeMatch
                    {
                        Index = fieldStart,
                        Length = i - fieldStart
                    });

                    fieldStart = i + 1;
                }
                else if (c == '(')
                {
                    bracketLeft++;
                }
                else if (c == ')')
                {
                    bracketRight++;

                    goto label_check;
                }
                else if (IsWhitespace(c))
                {
                    flag = true;

                    goto label_check;
                }
                else if (flag)
                {
                    flag = false;

                    letterStart = i;
                }

                continue;

label_check:
                if (bracketLeft == bracketRight && letterStart > 0)
                {
                    int offset = i - letterStart;

                    if (offset == 4 && IsFrom(sql, letterStart)) //? from
                    {
                        break;
                    }

                    if (distinctFlag && offset == 8 && IsDistinct(sql, letterStart))
                    {
                        fieldStart = startIndex = i;

                        distinctFlag = false;
                    }
                }
                else if (startIndex == 0)
                {
                    startIndex = i;
                }
            }

            letterStart -= 1;//? 去除最后一个占位符。

            matches.Add(new RangeMatch
            {
                Index = fieldStart,
                Length = letterStart - fieldStart
            });

            return Tuple.Create(startIndex, letterStart);
        }

        private static string MakeName(string str) => Regex.Replace(str, "[^\\w]", "_");

        /// <summary>
        /// SQL。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="take">获取“<paramref name="take"/>”条数据。</param>
        /// <param name="skip">跳过“<paramref name="skip"/>”条数据。</param>
        /// <param name="orderBy">排序。</param>
        /// <returns></returns>
        public virtual string ToSQL(string sql, int take, int skip, string orderBy)
        {
            var withAsMt = PatternWithAs.Match(sql);

            if (skip < 1)
            {
                if (withAsMt.Success)
                {
                    int startIndex = withAsMt.Index + withAsMt.Length;

                    return string.Concat(PatternSelect.Replace(sql, x =>
                    {
                        return string.Concat(x.Value, "TOP (", take.ToString(), ") ");
                    }, sql.Length - startIndex, startIndex), orderBy);
                }

                return string.Concat(PatternSelect.Replace(sql, x =>
                {
                    return string.Concat(x.Value, "TOP (", take.ToString(), ") ");
                }), orderBy);
            }

            var sb = new StringBuilder();

            if (withAsMt.Success)
            {
                int startIndex = withAsMt.Index + withAsMt.Length;

                sb.Append(sql, 0, startIndex);

                sql = sql.Substring(startIndex);
            }

            var tuple = AnalysisFields(sql, out List<RangeMatch> matches);

            string row_name = Name("__Row_Number_");

            if (orderBy.IsEmpty())
            {
                orderBy = " ORDER BY 1";
            }

            if (matches.Count > 1)
            {
                StringBuilder sbFields = new StringBuilder();

                sb.Append("SELECT ");

                for (int i = 0; i < matches.Count; i++)
                {
                    var item = matches[i];

                    var match = PatternSingleAsColumn.Match(sql, item.Index, item.Length);

                    if (i > 0)
                    {
                        sb.Append(',');
                        sbFields.Append(',');
                    }

                    sbFields.Append(sql, item.Index, item.Length);

                    if (match.Success)
                    {
                        sb.Append(sql, match.Index, match.Length);
                    }
                    else if (PatternAnyField.Match(sql, item.Index, item.Length).Success)
                    {
                        sb.Length = 0;

                        sb.Append("SELECT * FROM (")
                            .Append(sql, 0, tuple.Item2);

                        goto label_core;
                    }
                    else
                    {
                        string name = Name(MakeName(sql.Substring(item.Index, item.Length)));

                        sb.Append(name);

                        sbFields.Append(" AS ")
                            .Append(name);
                    }
                }

                sb.Append(" FROM (")
                    .Append(sql, 0, tuple.Item1)
                    .Append(sbFields.ToString());
            }
            else
            {
                sb.Append("SELECT * FROM (")
                    .Append(sql, 0, tuple.Item2);
            }

label_core:

            sb.Append(',')
                .Append("ROW_NUMBER() OVER(")
                .Append(orderBy)
                .Append(") AS ")
                .Append(row_name)
#if NETSTANDARD2_1_OR_GREATER
                .Append(sql[tuple.Item2..])
#else
                .Append(sql.Substring(tuple.Item2))
#endif
                .Append(") ")
                .Append(Name("xTables"))
                .Append(" WHERE ")
                .Append(row_name)
                .Append(" > ")
                .Append(skip);

            if (take > 0)
            {
                sb.Append(" AND ")
                    .Append(row_name)
                    .Append(" <= ")
                    .Append(skip + take);
            }

            return sb.ToString();
        }
    }
}

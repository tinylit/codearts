using CodeArts.Db.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeArts.Db.Oracle
{
    /// <summary>
    /// Oracle 矫正配置。
    /// </summary>
    public class OracleCorrectSettings : ISQLCorrectSettings
    {
        private static readonly ConcurrentDictionary<string, Tuple<string, bool>> mapperCache = new ConcurrentDictionary<string, Tuple<string, bool>>();

        private static readonly Regex PatternSelect = new Regex(@"^[\x20\t\r\n\f]*select([\x20\t\r\n\f]+distinct)+?[\x20\t\r\n\f]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PatternSingleAsColumn = new Regex(@"([\x20\t\r\n\f]+as[\x20\t\r\n\f]+)?(\w+\.)*(?<name>(\w+))[\x20\t\r\n\f]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.RightToLeft);

        private List<ICustomVisitor> visitters;

        /// <summary>
        /// 访问器。
        /// </summary>
        public IList<ICustomVisitor> Visitors => visitters ?? (visitters = new List<ICustomVisitor>());

        /// <summary>
        /// 字符串截取。 SUBSTR。
        /// </summary>
        public string Substring => "SUBSTR";

        /// <summary>
        /// 字符串索引。 INSTR。
        /// </summary>
        public string IndexOf => "INSTR";

        /// <summary>
        /// 字符串长度。 LENGTH。
        /// </summary>
        public string Length => "LENGTH";

        /// <summary>
        /// 索引项是否交换位置。 false。
        /// </summary>
        public bool IndexOfSwapPlaces => false;

        /// <summary>
        /// Oracle。
        /// </summary>
        public DatabaseEngine Engine => DatabaseEngine.Oracle;

        private ICollection<IFormatter> formatters;
        /// <summary>
        /// 格式化集合。
        /// </summary>
        public ICollection<IFormatter> Formatters => formatters ?? (formatters = new List<IFormatter>());

        /// <summary>
        /// 字段。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public string Name(string name) => name;

        /// <summary>
        /// 参数名称。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public string ParamterName(string name) => string.Concat(":", name);

        private Tuple<string, bool> GetColumns(string columns) => mapperCache.GetOrAdd(columns, _ =>
        {
            var list = ToSingleColumnCodeBlock(columns);

            if (list.Count == 1)
            {
                var match = PatternSingleAsColumn.Match(list.First());

                if (match.Success)
                {
                    return Tuple.Create(match.Groups["name"].Value, false);
                }

                return Tuple.Create(Name("__oracle_col"), true);
            }

            return Tuple.Create(string.Join(",", list.ConvertAll(item =>
            {
                var match = PatternSingleAsColumn.Match(item);

                if (match.Success)
                {
                    return match.Groups["name"].Value;
                }

                throw new DException("分页且多字段时,必须指定字段名!");
            })), false);
        });

        /// <summary>
        /// 每列代码块（如:[x].[id],substring([x].[value],[x].[index],[x].[len]) as [total] => new List&lt;string&gt;{ "[x].[id]","substring([x].[value],[x].[index],[x].[len]) as [total]" }）。
        /// </summary>
        /// <param name="columns">以“,”分割的列集合。</param>
        /// <returns></returns>
        protected virtual List<string> ToSingleColumnCodeBlock(string columns) => CommonSettings.ToSingleColumnCodeBlock(columns);

        /// <summary>
        /// 查询字段。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <returns></returns>
        protected virtual Tuple<string, int> QueryFields(string sql) => CommonSettings.QueryFields(sql);

        private bool IsExistsCondition(string sql, int startIndex)
        {
            int indexOf = sql.IndexOf(" WHERE ", startIndex, StringComparison.OrdinalIgnoreCase);

            if (indexOf == -1)
            {
                return false;
            }

            int indexOfSelect = sql.IndexOf("SELECT ", startIndex, indexOf, StringComparison.OrdinalIgnoreCase);

            if (indexOfSelect == -1)
            {
                return true;
            }

            return sql.IndexOf('(', indexOfSelect - 1) == -1 || sql.IndexOf(' ', indexOfSelect - 1) == -1;
        }

        /// <summary>
        /// SQL。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="take">获取“<paramref name="take"/>”条数据。</param>
        /// <param name="skip">跳过“<paramref name="skip"/>”条数据。</param>
        /// <param name="orderBy">排序。</param>
        /// <returns></returns>
        public string ToSQL(string sql, int take, int skip, string orderBy)
        {
            Tuple<string, int> fields = QueryFields(sql);

            Tuple<string, bool> tuple = GetColumns(fields.Item1);

            string row_name = Name("__Row_Number_");

            var sb = new StringBuilder();

            int startIndex = fields.Item2 + fields.Item1.Length;

            if (orderBy.IsEmpty())
            {
                if (skip < 0)
                {
                    return sb.Append(sql)
                         .Append(IsExistsCondition(sql, startIndex) ? " AND " : " WHERE ")
                         .Append("ROWNUM")
                         .Append("<=")
                         .Append(take)
                         .ToString();
                }

                sb.Append("SELECT ")
                      .Append(tuple.Item1)
                      .Append(" FROM (");

                if (tuple.Item2)
                {
                    sb.Append(sql.Substring(0, startIndex))
                        .Append(" AS ")
                        .Append(tuple.Item1)
                        .Append(", ")
                        .Append("ROWNUM AS ")
                        .Append(row_name)
                        .Append(sql.Substring(startIndex));
                }
                else
                {
                    sb.Append(PatternSelect.Replace(sql, x =>
                    {
                        return string.Concat(x.Value, "ROWNUM AS ", row_name, ", ");
                    }));
                }

                return sb.Append(IsExistsCondition(sql, startIndex) ? " AND " : " WHERE ")
                      .Append("ROWNUM")
                      .Append("<=")
                      .Append(take + skip)
                      .Append(")")
                      .Append(Name("CTE_RowNumber"))
                      .Append(" WHERE ")
                      .Append(row_name)
                      .Append(">")
                      .Append(skip)
                      .ToString();
            }

            sb.Append("SELECT ")
                .Append(tuple.Item1);

            if (skip <= 0)
            {
                if (tuple.Item2)
                {
                    sb.Append(" FROM (")
                        .Append("SELECT ")
                        .Append(fields.Item1)
                        .Append(" AS ")
                        .Append(tuple.Item1);
                }

                sb.Append(", ")
                    .Append("ROWNUM AS ")
                    .Append(row_name)
                    .Append(" FROM (")
                    .Append(sql)
                    .Append(orderBy)
                    .Append(")")
                    .Append(Name("CTE"))
                    .Append(" WHERE ")
                    .Append(Name("ROWNUM"))
                    .Append("<=")
                    .Append(take);

                if (tuple.Item2)
                {
                    sb.Append(") ")
                        .Append(Name("CTE_RowNumber"));
                }

                return sb.ToString();
            }

            sb.Append("(")
                .Append(tuple.Item1)
                .Append(", ")
                .Append("ROWNUM")
                .Append(" AS ")
                .Append(row_name)
                .Append(" FROM (");

            if (tuple.Item2)
            {
                sb.Append(sql.Substring(0, startIndex))
                       .Append(" AS ")
                       .Append(tuple.Item1)
                       .Append(sql.Substring(startIndex));
            }
            else
            {
                sb.Append(sql);
            }

            return sb.Append(orderBy)
                  .Append(")")
                  .Append(Name("CTE"))
                  .Append(" WHERE ")
                  .Append("ROWNUM")
                  .Append("<=")
                  .Append(take + skip)
                  .Append(")")
                  .Append(Name("CTE_RowNumber"))
                  .Append(" WHERE ")
                  .Append(row_name)
                  .Append(">")
                  .Append(skip)
                  .ToString();
        }
    }
}

using CodeArts.ORM.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeArts.ORM.SqlServer
{
    /// <summary>
    /// SqlServer矫正设置
    /// </summary>
    public class SqlServerCorrectSettings : ISQLCorrectSettings
    {
        private static readonly ConcurrentDictionary<string, Tuple<string, bool>> mapperCache = new ConcurrentDictionary<string, Tuple<string, bool>>();

        private static readonly Regex PatternSelect = new Regex(@"^[\x20\t\r\n\f]*select[\x20\t\r\n\f]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex PatternSingleAsColumn = new Regex(@"([\x20\t\r\n\f]+as[\x20\t\r\n\f]+)?(\[\w+\]\.)*(?<name>(\[\w+\]))[\x20\t\r\n\f]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.RightToLeft);
   
        
        /// <summary>
        /// 字符串截取。 SUBSTRING
        /// </summary>
        public string Substring => "SUBSTRING";

        /// <summary>
        /// 字符串索引。 CHARINDEX
        /// </summary>
        public string IndexOf => "CHARINDEX";

        /// <summary>
        /// 字符串长度。 LEN
        /// </summary>
        public string Length => "LEN";

        /// <summary>
        /// 索引位置对调。 true.
        /// </summary>
        public bool IndexOfSwapPlaces => true;

        private List<ICustomVisitor> visitters;

        /// <summary>
        /// 格式化
        /// </summary>
        public IList<ICustomVisitor> Visitors => visitters ?? (visitters = new List<ICustomVisitor>());

        /// <summary>
        /// SqlServer
        /// </summary>
        public DatabaseEngine Engine => DatabaseEngine.SqlServer;


        private ICollection<IFormatter> formatters;
        /// <summary>
        /// 格式化集合
        /// </summary>
        public ICollection<IFormatter> Formatters => formatters ?? (formatters = new List<IFormatter>());

        /// <summary>
        /// 字段
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public string Name(string name) => string.Concat("[", name, "]");
        /// <summary>
        /// 参数名称
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public string ParamterName(string name) => string.Concat("@", name);

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

                return Tuple.Create(Name("__sql_server_col"), true);
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
        /// 每列代码块（如:[x].[id],substring([x].[value],[x].[index],[x].[len]) as [total] => new List&lt;string&gt;{ "[x].[id]","substring([x].[value],[x].[index],[x].[len]) as [total]" }）
        /// </summary>
        /// <param name="columns">以“,”分割的列集合</param>
        /// <returns></returns>
        protected virtual List<string> ToSingleColumnCodeBlock(string columns) => CommonSettings.ToSingleColumnCodeBlock(columns);

        /// <summary>
        /// 查询字段。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <returns></returns>
        protected virtual Tuple<string, int> QueryFields(string sql) => CommonSettings.QueryFields(sql);

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
            if (skip < 1)
            {
                return string.Concat(PatternSelect.Replace(sql, x =>
                {
                    return string.Concat(x.Value, "TOP ", take.ToString(), " ");
                }), orderBy);
            }

            Tuple<string, int> fields = QueryFields(sql);

            Tuple<string, bool> tuple = GetColumns(fields.Item1);

            string row_name = Name("__Row_Number_");

            var sb = new StringBuilder();

            sb.Append("SELECT ")
                .Append(tuple.Item1)
                .Append(" FROM (");

            if (tuple.Item2)
            {
                int startIndex = fields.Item2 + fields.Item1.Length;

                sb.Append(sql.Substring(0, startIndex))
                 .Append(" AS ")
                 .Append(tuple.Item1)
                 .Append(", ")
                 .Append("ROW_NUMBER() OVER(")
                 .Append(orderBy)
                 .Append(") AS ")
                 .Append(row_name)
                 .Append(sql.Substring(startIndex));
            }
            else
            {
                sb.Append(PatternSelect.Replace(sql, x =>
                {
                    return string.Concat(x.Value, "ROW_NUMBER() OVER(", orderBy, ") AS ", row_name, " ,");
                }));
            }

            sb.Append(") ")
                 .Append(Name("CTE"))
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

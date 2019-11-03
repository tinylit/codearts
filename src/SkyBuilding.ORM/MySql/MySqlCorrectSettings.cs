using SkyBuilding.ORM;
using SkyBuilding.ORM.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SkyBuilding.ORM.MySql
{
    /// <summary>
    /// MySQL 矫正配置
    /// </summary>
    public class MySqlCorrectSettings : ISQLCorrectSettings
    {
        private readonly static ConcurrentDictionary<string, Tuple<string, bool>> mapperCache = new ConcurrentDictionary<string, Tuple<string, bool>>();

        private readonly static Regex PatternColumn = new Regex("select\\s+(?<column>((?!select|where).)+(select((?!from|select).)+from((?!from|select).)+)*((?!from|select).)*)\\s+from\\s+", RegexOptions.IgnoreCase);

        private readonly static Regex PatternSingleAsColumn = new Regex(@"([\x20\t\r\n\f]+as[\x20\t\r\n\f]+)?(\[\w+\]\.)*(?<name>(\[\w+\]))[\x20\t\r\n\f]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.RightToLeft);

        /// <summary>
        /// 字符串截取。 SUBSTRING
        /// </summary>
        public string Substring => "SUBSTRING";

        /// <summary>
        /// 字符串索引。 LOCATE
        /// </summary>
        public string IndexOf => "LOCATE";

        /// <summary>
        /// 字符串长度。 LENGTH
        /// </summary>
        public string Length => "LENGTH";

        /// <summary>
        /// 索引项是否对调。 true
        /// </summary>
        public bool IndexOfSwapPlaces => true;

        private List<IVisitter> visitters;

        /// <summary>
        /// 访问器
        /// </summary>
        public IList<IVisitter> Visitters => visitters ?? (visitters = new List<IVisitter>());

        /// <summary>
        /// MySQL
        /// </summary>
        public DatabaseEngine Engine => DatabaseEngine.MySQL;
        /// <summary>
        /// 字段名称
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public string Name(string name) => string.Concat("`", name, "`");
        /// <summary>
        /// 别名
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public string AsName(string name) => Name(name);
        /// <summary>
        /// 表名称
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public string TableName(string name) => Name(name);
        /// <summary>
        /// 参数名称
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public string ParamterName(string name) => string.Concat("?", name);
        /// <summary>
        /// 分页
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="take">获取N条</param>
        /// <param name="skip">跳过M条</param>
        /// <returns></returns>
        public virtual string PageSql(string sql, int take, int skip)
        {
            var sb = new StringBuilder();

            sb.Append(sql)
                .Append(" LIMIT ");

            if (skip > 0)
            {
                sb.Append(skip)
                    .Append(",");
            }

            return sb.Append(take).ToString();
        }

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

                return Tuple.Create(AsName("__my_sql_col"), true);
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
        /// 分页（交集、并集等）
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="take">获取N条</param>
        /// <param name="skip">跳过M条</param>
        /// <param name="orderBy">排序</param>
        /// <returns></returns>
        public virtual string PageUnionSql(string sql, int take, int skip, string orderBy)
        {
            var sb = new StringBuilder();

            var match = PatternColumn.Match(sql);

            string value = match.Groups["column"].Value;

            Tuple<string, bool> tuple = GetColumns(value);

            if (tuple.Item2)
            {
                throw new DException("组合查询必须指定字段名!");
            }

            sb.Append("SELECT ")
                .Append(tuple.Item1)
                .Append(" FROM (")
                .Append(sql)
                .Append(") ")
                .Append(TableName("CTE"))
                .Append(" ")
                .Append(orderBy)
                .Append(" LIMIT ");

            if (skip > 0)
            {
                sb.Append(skip)
                    .Append(",");
            }

            return sb.Append(take).ToString();
        }
    }
}

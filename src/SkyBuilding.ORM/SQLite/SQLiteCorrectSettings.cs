using SkyBuilding.ORM.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SkyBuilding.ORM.SQLite
{
    /// <summary>
    /// SQLite 修正配置
    /// </summary>
    public class SQLiteCorrectSettings : ISQLCorrectSettings
    {
        private readonly static ConcurrentDictionary<string, Tuple<string, bool>> mapperCache = new ConcurrentDictionary<string, Tuple<string, bool>>();

        private readonly static Regex PatternColumn = new Regex("select\\s+(?<column>((?!select|where).)+(select((?!from|select).)+from((?!from|select).)+)*((?!from|select).)*)\\s+from\\s+", RegexOptions.IgnoreCase);

        private readonly static Regex PatternSingleAsColumn = new Regex(@"([\x20\t\r\n\f]+as[\x20\t\r\n\f]+)?(\[\w+\]\.)*(?<name>(\[\w+\]))[\x20\t\r\n\f]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.RightToLeft);

        private IList<IVisitter> visitters;
        /// <summary>
        /// 表达式分析
        /// </summary>
        public IList<IVisitter> Visitters => visitters ?? (visitters = new List<IVisitter>());

        /// <summary>
        /// SQLite
        /// </summary>
        public DatabaseEngine Engine => DatabaseEngine.SQLite;

        /// <summary>
        /// 字符串截取
        /// </summary>
        public string Substring => "SUBSTR";

        /// <summary>
        /// 字符串索引
        /// </summary>
        public string IndexOf => "INSTR";

        /// <summary>
        /// 字符串长度
        /// </summary>
        public string Length => "LENGTH";

        /// <summary>
        /// 内容反向
        /// </summary>
        public bool IndexOfSwapPlaces => false;

        /// <summary>
        /// 别名
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public string AsName(string name) => Name(name);

        /// <summary>
        /// 字段名称
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public string Name(string name) => string.Concat("[", name, "]");

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

            return sb.Append(sql)
                 .Append(" LIMIT ")
                 .Append(take)
                 .Append(" OFFSET ")
                 .Append(skip)
                 .ToString();
        }

        /// <summary>
        /// 每列代码块（如:[x].[id],substring([x].[value],[x].[index],[x].[len]) as [total] => new List&lt;string&gt;{ "[x].[id]","substring([x].[value],[x].[index],[x].[len]) as [total]" }）
        /// </summary>
        /// <param name="columns">以“,”分割的列集合</param>
        /// <returns></returns>
        protected virtual List<string> ToSingleColumnCodeBlock(string columns) => CommonSettings.ToSingleColumnCodeBlock(columns);

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

                return Tuple.Create(AsName("__sqlite_col"), true);
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
        /// 分页（并集、交集等）
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="take">获取N条</param>
        /// <param name="skip">跳过M条</param>
        /// <param name="orderBy">排序</param>
        /// <returns></returns>
        public string PageUnionSql(string sql, int take, int skip, string orderBy)
        {
            var sb = new StringBuilder();

            var match = PatternColumn.Match(sql);

            string value = match.Groups["column"].Value;

            Tuple<string, bool> tuple = GetColumns(value);

            if (tuple.Item2)
            {
                throw new DException("组合查询必须指定字段名!");
            }

            return sb.Append("SELECT ")
                 .Append(tuple.Item1)
                 .Append(" FROM (")
                 .Append(sql)
                 .Append(") ")
                 .Append(TableName("CTE"))
                 .Append(" ")
                 .Append(orderBy)
                 .Append(" LIMIT ")
                 .Append(take)
                 .Append(" OFFSET ")
                 .Append(skip)
                 .ToString();
        }

        /// <summary>
        /// 参数名称
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public string ParamterName(string name) => string.Concat("@", name);

        /// <summary>
        /// 表名称
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public string TableName(string name) => Name(name);
    }
}

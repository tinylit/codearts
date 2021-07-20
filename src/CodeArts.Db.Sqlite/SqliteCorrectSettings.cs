using System.Collections.Generic;
using System.Text;

namespace CodeArts.Db
{
    /// <summary>
    /// SQLite 修正配置。
    /// </summary>
    public class SqliteCorrectSettings : ISQLCorrectSettings
    {
        /// <summary>
        /// SQLite。
        /// </summary>
        public DatabaseEngine Engine => DatabaseEngine.SQLite;

        /// <summary>
        /// 字符串截取。
        /// </summary>
        public string Substring => "SUBSTR";

        /// <summary>
        /// 字符串索引。
        /// </summary>
        public string IndexOf => "INSTR";

        /// <summary>
        /// 字符串长度。
        /// </summary>
        public string Length => "LENGTH";

        /// <summary>
        /// 内容反向。
        /// </summary>
        public bool IndexOfSwapPlaces => false;

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
        /// 字段名称。
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
            var sb = new StringBuilder();

            return sb.Append(sql)
                  .Append(orderBy)
                  .Append(" LIMIT ")
                  .Append(take)
                  .Append(" OFFSET ")
                  .Append(skip)
                  .ToString();
        }
    }
}

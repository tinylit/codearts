using System.Collections.Generic;
using System.Text;

namespace CodeArts.Db
{
    /// <summary>
    /// MySQL 矫正配置。
    /// </summary>
    public class MySqlCorrectSettings : MySqlCorrectSimSettings, ISQLCorrectSettings, ISQLCorrectSimSettings
    {
        private List<ICustomVisitor> visitters;

        /// <summary>
        /// 格式化。
        /// </summary>
        public IList<ICustomVisitor> Visitors => visitters ?? (visitters = new List<ICustomVisitor>());

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
            var sb = new StringBuilder();

            sb.Append(sql)
                .Append(orderBy)
                .Append(" LIMIT ");

            if (skip > 0)
            {
                sb.Append(skip)
                    .Append(",");
            }

            return sb.Append(take)
                .ToString();
        }
    }
}

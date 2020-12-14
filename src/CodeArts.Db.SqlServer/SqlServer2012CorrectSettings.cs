using System;
using System.Text;

namespace CodeArts.Db
{
    /// <summary>
    /// SqlServer 2012 矫正设置。
    /// </summary>
    public class SqlServer2012CorrectSettings : SqlServerCorrectSettings
    {
        /// <summary>
        /// SQL。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="take">获取“<paramref name="take"/>”条数据。</param>
        /// <param name="skip">跳过“<paramref name="skip"/>”条数据。</param>
        /// <param name="orderBy">排序。</param>
        /// <returns></returns>
        public override string ToSQL(string sql, int take, int skip, string orderBy)
        {
            if (skip < 1)
            {
                return base.ToSQL(sql, take, skip, orderBy);
            }

            if (orderBy.IsEmpty())
            {
                orderBy = " ORDER BY 1";
            }

            var sb = new StringBuilder();

            sb.Append(sql)
               .Append(orderBy)
               .Append("OFFSET ")
               .Append(skip)
               .Append(" ROWS");

            if (take > 0)
            {
                sb.Append(" FETCH NEXT ")
                    .Append(take)
                    .Append(" ROWS ONLY");
            }

            return sb.ToString();
        }
    }
}

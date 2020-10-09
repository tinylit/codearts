using System.Collections.Generic;

namespace CodeArts.ORM
{
    /// <summary>
    /// SQL矫正设置
    /// </summary>
    public interface ISQLCorrectSettings : ISQLCorrectSimSettings
    {
        /// <summary>
        /// 访问器
        /// </summary>
        IList<ICustomVisitor> Visitors { get; }

        /// <summary>
        /// SQL(分页)。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="take">获取“<paramref name="take"/>”条数据。</param>
        /// <param name="skip">跳过“<paramref name="skip"/>”条数据。</param>
        /// <param name="orderBy">排序。</param>
        /// <returns></returns>
        string ToSQL(string sql, int take, int skip, string orderBy);
    }
}

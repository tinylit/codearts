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
        IList<IVisitor> Visitors { get; }

        /// <summary>
        /// SQL(分页)。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="take">获取“<paramref name="take"/>”条数据。</param>
        /// <param name="skip">跳过“<paramref name="skip"/>”条数据。</param>
        /// <param name="orderBy">排序。</param>
        /// <returns></returns>
        string ToSQL(string sql, int take, int skip, string orderBy);

        /// <summary>
        /// 构建TakeSkip语句
        /// </summary>
        /// <param name="sql">原SQL语句</param>
        /// <param name="take">获取多少条数据</param>
        /// <param name="skip">跳过多少条数据</param>
        /// <returns></returns>
        string PageSql(string sql, int take, int skip);

        /// <summary>
        /// 构建TakeSkip语句(使用 UNION 或 UNION ALL的分页)
        /// </summary>
        /// <param name="sql">原SQL语句</param>
        /// <param name="take">获取多少条数据</param>
        /// <param name="skip">跳过多少条数据</param>
        /// <param name="orderBy">排序语句</param>
        /// <returns></returns>
        string PageUnionSql(string sql, int take, int skip, string orderBy);
    }
}

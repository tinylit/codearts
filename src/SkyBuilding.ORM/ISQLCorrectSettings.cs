using System.Collections.Generic;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// SQL矫正设置
    /// </summary>
    public interface ISQLCorrectSettings : ISQLCorrectSimSettings
    {
        /// <summary>
        /// 访问器
        /// </summary>
        IList<IVisitter> Visitters { get; }

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

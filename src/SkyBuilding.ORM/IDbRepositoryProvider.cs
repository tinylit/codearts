using System.Data;
using System.Linq.Expressions;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 仓储提供者
    /// </summary>
    public interface IDbRepositoryProvider
    {
        /// <summary>
        /// 创建查询器
        /// </summary>
        /// <returns></returns>
        IQueryBuilder Create();

        /// <summary>
        /// 创建执行器
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <returns></returns>
        IBuilder<T> Create<T>();

        /// <summary>
        /// 测评表达式语句（查询）
        /// </summary>
        /// <typeparam name="T">仓储泛型类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="connection">数据库链接</param>
        /// <param name="expression">查询表达式</param>
        /// <returns></returns>
        TResult Evaluate<T, TResult>(IDbConnection conn, Expression expression);
    }
}

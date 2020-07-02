using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace CodeArts.ORM
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
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">SQL</param>
        /// <param name="parameters">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        T QueryFirstOrDefault<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, int? commandTimeout = null);

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">SQL</param>
        /// <param name="parameters">参数</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <param name="missingMsg">未查询到数据时，异常信息。</param>
        /// <returns></returns>
        T QueryFirst<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, T defaultValue = default, int? commandTimeout = null, string missingMsg = null);

        /// <summary>
        /// 查询列表集合
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">查询语句</param>
        /// <param name="parameters">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, int? commandTimeout = null);

        /// <summary>
        /// 测评表达式语句（查询）
        /// </summary>
        /// <typeparam name="T">仓储泛型类型</typeparam>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="expression">查询表达式</param>
        /// <returns></returns>
        TResult Evaluate<T, TResult>(IDbConnection conn, Expression expression);
    }
}

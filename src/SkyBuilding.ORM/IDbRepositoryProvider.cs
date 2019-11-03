using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using SkyBuilding.ORM.Exceptions;

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
        /// 查询独立实体
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">查询语句</param>
        /// <param name="parameters">参数</param>
        /// <param name="required">是否必须</param>
        /// <param name="defaultValue">默认值</param>
        /// <exception cref="DRequiredException">当 【required】 为真且未查询到数据</exception>
        /// <returns></returns>
        T QueryFirst<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null, bool required = false, T defaultValue = default);

        /// <summary>
        /// 查询列表集合
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="conn">数据库链接</param>
        /// <param name="sql">查询语句</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(IDbConnection conn, string sql, Dictionary<string, object> parameters = null);

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

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据上下文。
    /// </summary>
    public interface IDbContext
    {
        /// <summary> 连接名称。 </summary>
        string Name { get; }

        /// <summary> 数据库驱动名称。 </summary>
        string ProviderName { get; }

        /// <summary>
        /// SQL 矫正。
        /// </summary>
        ISQLCorrectSettings Settings { get; }

        /// <summary>
        /// 创建数据库链接。
        /// </summary>
        /// <returns></returns>
        DbConnection CreateDb();

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        T Read<T>(Expression expression);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(Expression expression);

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="hasDefaultValue">是否包含默认值。</param>
        /// <param name="defaultValue">默认值（仅“<paramref name="hasDefaultValue"/>”为真时，有效）。</param>
        /// <param name="missingMsg">未查询到数据时，异常信息。</param>
        /// <returns></returns>
        T Read<T>(SQL sql, object param = null, int? commandTimeout = null, bool hasDefaultValue = true, T defaultValue = default, string missingMsg = null);

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(SQL sql, object param = null, int? commandTimeout = null);

#if NET_NORMAL || NETSTANDARD2_0

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> ReadAsync<T>(Expression expression, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        IAsyncEnumerable<T> QueryAsync<T>(Expression expression);

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="hasDefaultValue">是否包含默认值。</param>
        /// <param name="defaultValue">默认值（仅“<paramref name="hasDefaultValue"/>”为真时，有效）。</param>
        /// <param name="missingMsg">未查询到数据时，异常信息。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> ReadAsync<T>(SQL sql, object param = null, int? commandTimeout = null, bool hasDefaultValue = true, T defaultValue = default, string missingMsg = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        IAsyncEnumerable<T> QueryAsync<T>(SQL sql, object param = null, int? commandTimeout = null);
#endif
    }

    /// <summary>
    /// 数据上下文。
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IDbContext<TEntity> : IDbContext where TEntity : class, IEntiy
    {
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        int Execute(Expression expression);

#if NET_NORMAL || NETSTANDARD2_0
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default);
#endif

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns>执行影响行。</returns>
        int Execute(SQL sql, object param = null, int? commandTimeout = null);

#if NET_NORMAL || NETSTANDARD2_0
        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>执行影响行。</returns>
        Task<int> ExecuteAsync(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
#endif

    }
}

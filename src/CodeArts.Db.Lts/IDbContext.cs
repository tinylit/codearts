using System;
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
        /// 使用事务。
        /// </summary>
        /// <typeparam name="T">返回结果。</typeparam>
        /// <param name="inTransactionExecution">事务中执行。</param>
        /// <returns></returns>
        T Transaction<T>(Func<IDbCommandFactory, T> inTransactionExecution);

        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <typeparam name="T">返回结果。</typeparam>
        /// <param name="inTransactionExecution">事务中执行。</param>
        /// <param name="isolationLevel">事务隔离级别。</param>
        /// <returns></returns>
        T Transaction<T>(Func<IDbCommandFactory, T> inTransactionExecution, System.Data.IsolationLevel isolationLevel);

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

#if NET_NORMAL || NET_CORE

        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <typeparam name="T">返回结果。</typeparam>
        /// <param name="inTransactionExecution">事务中执行。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> TransactionAsync<T>(Func<IDbCommandFactory, Task<T>> inTransactionExecution, CancellationToken cancellationToken = default);

        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <typeparam name="T">返回结果。</typeparam>
        /// <param name="inTransactionExecution">事务中执行。</param>
        /// <param name="isolationLevel">事务隔离级别。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> TransactionAsync<T>(Func<IDbCommandFactory, Task<T>> inTransactionExecution, System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

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

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        int Execute(Expression expression);

#if NET_NORMAL || NET_CORE
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
        /// <remarks>校验数据表</remarks>
        int Execute<T>(SQL sql, object param = null, int? commandTimeout = null) where T : class, IEntiy;

#if NET_NORMAL || NET_CORE
        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>执行影响行。</returns>
        /// <remarks>校验数据表</remarks>
        Task<int> ExecuteAsync<T>(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where T : class, IEntiy;
#endif
    }
}

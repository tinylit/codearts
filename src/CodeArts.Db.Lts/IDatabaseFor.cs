using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 表达式。
    /// </summary>
    public interface IDatabaseFor
    {
        /// <summary>
        /// 分析读取SQL。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        T Read<T>(string connectionString, Expression expression);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回元素类型。</typeparam>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(string connectionString, Expression expression);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> ReadAsync<T>(string connectionString, Expression expression, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回元素类型。</typeparam>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        IAsyncEnumerable<T> QueryAsync<T>(string connectionString, Expression expression);
#endif

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        int Execute(string connectionString, Expression expression);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> ExecuteAsync(string connectionString, Expression expression, CancellationToken cancellationToken = default);
#endif
    }
}

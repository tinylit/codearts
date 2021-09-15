#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据库（开启事务后，创建的<see cref="IDbCommand"/>会自动设置事务。）。
    /// </summary>
    public partial interface IDatabase : IDbConnection
    {
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
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="commandSql">查询语句。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> ReadAsync<T>(CommandSql<T> commandSql, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="missingMsg">未找到数据异常。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> SingleAsync<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> SingleOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="missingMsg">未找到数据异常。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> FirstAsync<T>(string sql, object param = null, string missingMsg = null, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> FirstOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, T defaultValue = default, CancellationToken cancellationToken = default);


        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="commandSql">查询语句。</param>
        /// <returns></returns>
        IAsyncEnumerable<T> QueryAsync<T>(CommandSql commandSql);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        IAsyncEnumerable<T> QueryAsync<T>(string sql, object param = null, int? commandTimeout = null);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="commandSql">执行语句。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> ExecuteAsync(CommandSql commandSql, CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> ExecuteAsync(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default);
    }
}
#endif
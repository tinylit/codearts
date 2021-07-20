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
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        CommandSql<T> Read<T>(Expression expression);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">查询语句。</param>
        /// <returns></returns>
        T Single<T>(IDbConnection connection, CommandSql<T> commandSql);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">查询语句。</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(IDbConnection connection, CommandSql commandSql);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">查询语句。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> SingleAsync<T>(IDbConnection connection, CommandSql<T> commandSql, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">查询语句。</param>
        /// <returns></returns>
        IAsyncEnumerable<T> QueryAsync<T>(IDbConnection connection, CommandSql commandSql);
#endif

        /// <summary>
        /// 生成执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        CommandSql Execute(Expression expression);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">执行语句。</param>
        /// <returns></returns>
        int Execute(IDbConnection connection, CommandSql commandSql);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">执行语句。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> ExecuteAsync(IDbConnection connection, CommandSql commandSql, CancellationToken cancellationToken = default);
#endif
    }
}

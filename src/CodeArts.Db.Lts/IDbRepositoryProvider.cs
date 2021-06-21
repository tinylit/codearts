using System.Collections.Generic;
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
using System.Data;
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 仓储提供者。
    /// </summary>
    public interface IDbRepositoryProvider
    {
        /// <summary>
        /// 创建查询器。
        /// </summary>
        /// <returns></returns>
        IQueryVisitor Create();

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns></returns>
        T Read<T>(IDbContext context, CommandSql<T> commandSql);

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(IDbContext context, CommandSql commandSql);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<T> ReadAsync<T>(IDbContext context, CommandSql<T> commandSql, CancellationToken cancellationToken = default);

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="context">数据库上下文。</param>
        /// <param name="commandSql">命令SQL。</param>
        /// <returns></returns>
        IAsyncEnumerable<T> QueryAsync<T>(IDbContext context, CommandSql commandSql);
#endif
    }
}

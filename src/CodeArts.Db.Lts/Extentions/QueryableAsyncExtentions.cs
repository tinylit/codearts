#if NET_NORMAL || NET_CORE
using CodeArts;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace System.LinqAsync
{
    /// <summary>
    /// 异步查询扩展。
    /// </summary>
    public static class QueryableAsyncExtentions
    {
        /// <summary>
        /// 转为集合。
        /// </summary>
        /// <typeparam name="T">源。</typeparam>
        /// <param name="source">源。</param>
        /// <param name="predicate">条件。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
            => source.Where(predicate).ToListAsync(cancellationToken);

        /// <summary>
        /// 转分页数据。
        /// </summary>
        /// <typeparam name="T">源。</typeparam>
        /// <param name="source">源。</param>
        /// <param name="page">页码（索引从“0”开始）。</param>
        /// <param name="size">分页条数。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public static async Task<PagedList<T>> ToListAsync<T>(this IQueryable<T> source, int page, int size, CancellationToken cancellationToken = default)
        {
            var count_task = source
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            var result_task = source
                .Skip(size * page)
                .Take(size)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new PagedList<T>(await result_task, page, size, await count_task);
        }
    }
}
#endif
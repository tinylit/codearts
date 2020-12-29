#if NET_NORMAL || NET_CORE
using System.Threading;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    /// <summary>
    /// 迭代扩展。
    /// </summary>
    public static class IAsyncEnumerableExtentions
    {
        /// <summary>
        /// 异步迭代转集合。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="cancellationToken">取消。</param>
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
        {
            var list = new List<T>();

            var enumerator = source.GetAsyncEnumerator(cancellationToken);

            while (await enumerator.MoveNextAsync())
            {
                list.Add(enumerator.Current);
            }

            return list;
        }

        /// <summary>
        /// 异步迭代转数组。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="cancellationToken">取消。</param>
        public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
            => (await source.ToListAsync(cancellationToken)).ToArray();

        /// <summary>
        /// 异步迭代。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="action">动作。</param>
        /// <param name="cancellationToken">取消。</param>
        public static async Task ForEachAsync<T>(this IAsyncEnumerable<T> source, Action<T> action, CancellationToken cancellationToken = default)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var enumerator = source
                .GetAsyncEnumerator(cancellationToken);

            while (await enumerator.MoveNextAsync())
            {
                action.Invoke(enumerator.Current);
            }
        }
    }
}
#endif
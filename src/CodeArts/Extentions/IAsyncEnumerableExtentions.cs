#if NET_NORMAL || NETSTANDARD2_0
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

            var enumerator = source.GetAsyncEnumerator();

            while (await enumerator.MoveNext(cancellationToken))
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
    }
}
#endif
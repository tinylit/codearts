#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
using System.Threading.Tasks;

namespace System.Collections
{
    /// <summary>
    /// 迭代扩展。
    /// </summary>
    public static class IEnumerableAsyncExtentions
    {
        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static async Task ForEachAsync<T>(this IEnumerable source, Func<T, Task> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (T item in source)
            {
                await action.Invoke(item);
            }
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static async Task ForEachAsync<T>(this IEnumerable source, Func<T, int, Task> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            int index = -1;

            foreach (T item in source)
            {
                await action.Invoke(item, ++index);
            }
        }
    }
}

namespace System.Collections.Generic
{
    /// <summary>
    /// 迭代扩展。
    /// </summary>
    public static class IEnumerableAsyncExtentions
    {
        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (T item in source)
            {
                await action.Invoke(item);
            }
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, int, Task> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            int index = -1;

            foreach (T item in source)
            {
                await action.Invoke(item, ++index);
            }
        }
    }
}
#endif
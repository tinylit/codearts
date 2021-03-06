﻿#if !NET40
using System.Collections.Generic;
using System.Text;
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
        public static Task ForEachAsync<T>(this IEnumerable source, Func<T, Task> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var tasks = new List<Task>();

            foreach (T item in source)
            {
                tasks.Add(action.Invoke(item));
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static Task ForEachAsync<T>(this IEnumerable source, Func<T, int, Task> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            int index = -1;

            var tasks = new List<Task>();

            foreach (T item in source)
            {
                tasks.Add(action.Invoke(item, ++index));
            }

            return Task.WhenAll(tasks);
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
        public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var tasks = new List<Task>();

            foreach (T item in source)
            {
                tasks.Add(action.Invoke(item));
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, int, Task> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            int index = -1;

            var tasks = new List<Task>();

            foreach (T item in source)
            {
                tasks.Add(action.Invoke(item, ++index));
            }

            return Task.WhenAll(tasks);
        }
    }
}
#endif
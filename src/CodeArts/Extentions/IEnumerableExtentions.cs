using System.Collections.Generic;
using System.Text;

namespace System.Collections
{
    /// <summary>
    /// 迭代扩展
    /// </summary>
    public static class IEnumerableExtentions
    {
        /// <summary>
        /// 字符串拼接
        /// </summary>
        /// <param name="source">数据源</param>
        /// <param name="separator">分隔符</param>
        /// <returns></returns>
        public static string Join(this IEnumerable source, string separator = ",")
        {
            var enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                return string.Empty;
            }

            while (enumerator.Current is null)
            {
                if (!enumerator.MoveNext())
                {
                    return string.Empty;
                }
            }

            var sb = new StringBuilder();

            sb.Append(enumerator.Current);

            while (enumerator.MoveNext())
            {
                if (enumerator.Current is null)
                {
                    continue;
                }

                sb.Append(separator)
                    .Append(enumerator.Current);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static void ForEach<T>(this IEnumerable source, Action<T> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (source is IList<T> list)
            {
                list.ForEach(action);
            }
            else
            {
                foreach (T item in source)
                {
                    action.Invoke(item);
                }
            }
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static void ForEach<T>(this IEnumerable source, Action<T, int> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            int index = -1;

            foreach (T item in source)
            {
                action.Invoke(item, ++index);
            }
        }
    }
}

namespace System.Collections.Generic
{
    /// <summary>
    /// 迭代扩展
    /// </summary>
    public static class IEnumerableExtentions
    {
        /// <summary>
        /// 对数据中的每个元素执行指定操作
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
            {
                action.Invoke(item);
            }
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="action">要对数据源的每个元素执行的委托。</param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            int index = -1;
            foreach (T item in source)
            {
                action.Invoke(item, ++index);
            }
        }
    }
}

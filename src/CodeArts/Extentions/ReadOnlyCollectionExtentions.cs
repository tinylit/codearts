#if NET40

namespace System.Collections.Generic
{
    /// <summary>
    /// 集合扩展。
    /// </summary>
    public static class ReadOnlyCollectionExtentions
    {
        /// <summary>
        /// 只读集合。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        private class ReadOnlyList<T> : IReadOnlyList<T>
        {
            private readonly IList<T> arrays;

            /// <summary>
            /// 初始化 System.Collections.Generic.ReadOnlyList`1 类的新实例，该实例包含从指定集合复制的元素并且具有足够的容量来容纳所复制的元素。
            /// </summary>
            /// <param name="arrays">元素集合。</param>
            public ReadOnlyList(IList<T> arrays)
            {
                if (arrays is null)
                {
                    throw new ArgumentNullException(nameof(arrays));
                }

                this.arrays = arrays;
            }

            /// <summary>
            /// 获取或设置指定索引处的元素。
            /// </summary>
            /// <param name="index">要获取或设置的元素的从零开始的索引。</param>
            /// <exception cref="ArgumentOutOfRangeException">index 小于 0。 或 - index 等于或大于 System.Collections.Generic.ReadOnlyList`1.Count。</exception>
            /// <returns>要获取或设置的元素的从零开始的索引。</returns>
            public T this[int index] => arrays[index];

            /// <summary>
            /// 获取 <see cref="ReadOnlyList{T}"/> 中包含的元素数。
            /// </summary>
            public int Count => arrays.Count;

            /// <summary>
            /// 返回循环访问 <see cref="ReadOnlyList{T}"/> 的枚举数。
            /// </summary>
            /// <returns></returns>
            public IEnumerator<T> GetEnumerator() => arrays.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// 表示键/值对的泛型只读集合。
        /// </summary>
        /// <typeparam name="TKey">只读字典中的键的类型。</typeparam>
        /// <typeparam name="TValue">只读字典中的值的类型。</typeparam>
        private class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        {
            private readonly IDictionary<TKey, TValue> dictionary;

            /// <summary>
            /// 初始化 <see cref="ReadOnlyDictionary{TKey, TValue}"/> 类的新实例，该实例包含从指定的 <see cref="IReadOnlyDictionary{TKey, TValue}"/> 复制的元素并为键类型使用默认的相等比较器。
            /// </summary>
            /// <param name="dictionary"> <see cref="IReadOnlyDictionary{TKey, TValue}"/>，它的元素被复制到新 <see cref="ReadOnlyDictionary{TKey, TValue}"/>。</param>
            /// <exception cref="ArgumentNullException"><paramref name="dictionary"/>为 null。</exception>
            public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
            {
                if (dictionary is null)
                {
                    throw new ArgumentNullException(nameof(dictionary));
                }

                this.dictionary = dictionary;
            }

            /// <summary>
            /// 获取在只读目录中有指定键的元素。
            /// </summary>
            /// <param name="key">要定位的键。</param>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
            /// <exception cref="KeyNotFoundException">检索了属性但没有找到 <paramref name="key"/>。</exception>
            /// <returns>在只读目录中有指定键的元素。</returns>
            public TValue this[TKey key] => dictionary[key];

            /// <summary>
            /// 获取包含只读字典中的键的可枚举集合。
            /// </summary>
            public IEnumerable<TKey> Keys => dictionary.Keys;

            /// <summary>
            /// 获取包含只读字典中的值的可枚举集合。
            /// </summary>
            public IEnumerable<TValue> Values => dictionary.Values;

            /// <summary>
            /// 获取包含在 <see cref="ReadOnlyDictionary{TKey, TValue}"/> 中的键/值对的数目。
            /// </summary>
            public int Count => dictionary.Count;

            /// <summary>
            /// 确定只读字典是否包含具有指定键的元素。
            /// </summary>
            /// <param name="key">要定位的键。</param>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
            /// <returns> 如果该只读词典包含一具有指定键的元素，则为 true；否则为 false。</returns>
            public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);

            /// <summary>
            /// 返回循环访问 <see cref="ReadOnlyDictionary{TKey, TValue}"/> 的枚举数。
            /// </summary>
            /// <returns>用于 <see cref="ReadOnlyDictionary{TKey, TValue}"/> 的元素迭代。</returns>
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();

            /// <summary>
            /// 获取与指定的键关联的值。
            /// </summary>
            /// <param name="key">要定位的键。</param>
            /// <param name="value">当此方法返回时，如果找到指定键，则返回与该键相关联的值；否则，将返回 value 参数的类型的默认值。 此参数未经初始化即被传递。</param>
            /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
            /// <returns>如果实现 System.Collections.Generic.IReadOnlyDictionary`2 接口的对象包含具有指定键的元素，则为 true；否则为 false。</returns>
            public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// 作为只读集合。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">元素集合。</param>
        /// <returns></returns>
        public static IReadOnlyList<T> ToReadOnlyList<T>(this IList<T> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ReadOnlyList<T>(source);
        }

        /// <summary>
        /// 作为只读集合。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">元素集合。</param>
        /// <returns></returns>
        public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ReadOnlyList<T>(new List<T>(source));
        }

        /// <summary>
        /// 作为只读字典。
        /// </summary>
        /// <typeparam name="TKey">只读字典中的键的类型。</typeparam>
        /// <typeparam name="TValue">只读字典中的值的类型。</typeparam>
        /// <param name="source">字典集合。</param>
        /// <returns></returns>
        public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IDictionary<TKey, TValue> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new ReadOnlyDictionary<TKey, TValue>(source);
        }
    }
}
#endif
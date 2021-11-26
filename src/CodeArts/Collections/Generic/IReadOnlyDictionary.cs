#if NET40
namespace System.Collections.Generic
{
    /// <summary>
    /// 表示键/值对的泛型只读集合。
    /// </summary>
    /// <typeparam name="TKey">只读字典中的键的类型。</typeparam>
    /// <typeparam name="TValue">只读字典中的值的类型。</typeparam>
    public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    {
        /// <summary>
        /// 获取在只读目录中有指定键的元素。
        /// </summary>
        /// <param name="key">要定位的键。</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
        /// <exception cref="KeyNotFoundException">检索了属性但没有找到 <paramref name="key"/>。</exception>
        /// <returns>在只读目录中有指定键的元素。</returns>
        TValue this[TKey key] { get; }

        /// <summary>
        /// 获取包含只读字典中的键的可枚举集合。
        /// </summary>
        IEnumerable<TKey> Keys { get; }

        /// <summary>
        /// 获取包含只读字典中的值的可枚举集合。
        /// </summary>
        IEnumerable<TValue> Values { get; }

        /// <summary>
        /// 确定只读字典是否包含具有指定键的元素。
        /// </summary>
        /// <param name="key">要定位的键。</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
        /// <returns> 如果该只读词典包含一具有指定键的元素，则为 true；否则为 false。</returns>
        bool ContainsKey(TKey key);

        /// <summary>
        /// 获取与指定的键关联的值。
        /// </summary>
        /// <param name="key">要定位的键。</param>
        /// <param name="value">当此方法返回时，如果找到指定键，则返回与该键相关联的值；否则，将返回 value 参数的类型的默认值。 此参数未经初始化即被传递。</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> 为 null。</exception>
        /// <returns>如果实现 System.Collections.Generic.IReadOnlyDictionary`2 接口的对象包含具有指定键的元素，则为 true；否则为 false。</returns>
        bool TryGetValue(TKey key, out TValue value);
    }
}
#endif
namespace System.Collections.Generic
{
    /// <summary>
    /// 字段扩展
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// 尝试将指定的键和值添加到 System.Collections.Generic.Dictionary`2 中。
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="valuePairs">字段集合</param>
        /// <param name="key">要添加的元素的键。</param>
        /// <param name="value">要添加的元素的值。 对于引用类型，该值可以为 null。</param>
        /// <returns>如果成功地将键/值对添加到 true，则为 System.Collections.Concurrent.ConcurrentDictionary`2；如果该键已存在，则为 false.</returns>
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> valuePairs, TKey key, TValue value)
        {
            if (valuePairs.ContainsKey(key))
                return false;

            valuePairs[key] = value;

            return true;
        }

        /// <summary>
        /// 如果该键不存在，System.Collections.Generic.Dictionary`2 中。 如果该键存在，则返回新值或现有值。
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="valuePairs">字段集合</param>
        /// <param name="key">要添加的元素的键。</param>
        /// <param name="value">要添加的元素的值。 对于引用类型，该值可以为 null。</param>
        /// <returns>键的值。 如果字典中已存在该键，则为该键的现有值；如果字典中不存在该键，则为新值。</returns>
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> valuePairs, TKey key, TValue value)
        {
            if (valuePairs.TryGetValue(key, out TValue oldValue))
                return oldValue;

            valuePairs[key] = value;

            return value;
        }

        /// <summary>
        /// 如果该键不存在，则通过使用指定的函数将键/值对添加到 System.Collections.Generic.Dictionary`2 中。 如果该键存在，则返回新值或现有值。
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="valuePairs">字段集合</param>
        /// <param name="key">要添加的元素的键。</param>
        /// <param name="valueFactory">用于为键生成值的函数。</param>
        /// <returns>键的值。 如果字典中已存在该键，则为该键的现有值；如果字典中不存在该键，则为新值。</returns>
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> valuePairs, TKey key, Func<TKey, TValue> valueFactory)
        {
            if (valuePairs.TryGetValue(key, out TValue value))
                return value;

            if (valueFactory is null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }

            valuePairs[key] = value = valueFactory.Invoke(key);

            return value;
        }
    }
}

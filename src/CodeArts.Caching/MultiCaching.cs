using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts.Caching
{
    /// <summary>
    /// 多级缓存。
    /// </summary>
    public sealed class MultiCaching : BaseCaching
    {
        private readonly List<ICaching> cachings;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="cachings">缓存集合。</param>
        public MultiCaching(List<ICaching> cachings)
        {
            this.cachings = cachings;
        }

        /// <summary> 缓存区域。 </summary>
        public override string Region { get; }

        /// <summary>
        /// 清除缓存。
        /// </summary>
        /// <returns></returns>
        public override void Clear() => cachings.ForEach(caching => caching.Clear());

        /// <summary>
        /// 指定键是否存在。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override bool IsExsits(string key) => cachings.TrueForAll(caching => caching.IsExsits(key));

        /// <summary>
        /// 设置有效时间。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="span">有效时间。</param>
        /// <returns></returns>
        public override bool Expire(string key, TimeSpan span) => cachings.TrueForAll(caching => caching.Expire(key, span));

        /// <summary>
        /// 获取键值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override object Get(string key)
        {
            foreach (var caching in cachings)
            {
                if (caching.IsExsits(key))
                {
                    return caching.Get(key);
                }
            }

            return null;
        }

        /// <summary>
        /// 获取键值。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override T Get<T>(string key)
        {
            foreach (var caching in cachings)
            {
                if (caching.IsExsits(key))
                {
                    return caching.Get<T>(key);
                }
            }

            return default;
        }

        /// <summary>
        /// 清除制定键的缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override void Remove(string key) => cachings.ForEach(caching => caching.Remove(key));

        /// <summary>
        /// 清除缓存。
        /// </summary>
        /// <param name="keys">键集合。</param>
        /// <returns></returns>
        public override void Remove(IEnumerable<string> keys)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            var list = keys
                    .Distinct()
                    .ToList();

            if (list.Count == 0)
            {
                return;
            }

            cachings.ForEach(caching => caching.Remove(list));
        }

        /// <summary>
        /// 设置缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <returns></returns>
        public override bool Set(string key, object value) => cachings.TrueForAll(caching => caching.Set(key, value));

        /// <summary>
        /// 设置缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <param name="span">过期时间。</param>
        /// <returns></returns>
        public override bool Set(string key, object value, TimeSpan span) => cachings.TrueForAll(caching => caching.Set(key, value, span));

        /// <summary>
        /// 加锁。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="timeSpan">锁定时间（默认：10s）。</param>
        public override bool TryEnter(string key, TimeSpan? timeSpan = null)
        {
            if (!timeSpan.HasValue)
            {
                timeSpan = TimeSpan.FromSeconds(10D);
            }

            for (int i = 0; i < cachings.Count; i++)
            {
                var caching = cachings[i];

                try
                {
                    if (caching.TryEnter(key, timeSpan))
                    {
                        continue;
                    }
                }
                catch
                {
                    for (int j = 0; j < i; j++)
                    {
                        caching.Exit(key);
                    }

                    throw;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// 退出锁。
        /// </summary>
        /// <param name="key">键。</param>
        public override void Exit(string key) => cachings.ForEach(caching => caching.Exit(key));

    }
}

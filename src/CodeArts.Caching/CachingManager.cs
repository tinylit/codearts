using CodeArts.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CodeArts.Caching
{
    /// <summary>
    /// 缓存管理。
    /// </summary>
    public static class CachingManager
    {
        /// <summary>
        /// 缓存供应商。
        /// </summary>
        private static readonly Dictionary<Level, ICachingProvider> CacheProvider;

        /// <summary>
        /// 缓存集合。
        /// </summary>
        private static readonly ConcurrentDictionary<ICachingProvider, ConcurrentDictionary<string, ICaching>> CacheDictionary;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static CachingManager()
        {
            CacheProvider = new Dictionary<Level, ICachingProvider>();
            CacheDictionary = new ConcurrentDictionary<ICachingProvider, ConcurrentDictionary<string, ICaching>>();
        }

        /// <summary>
        /// 设置缓存供应器。
        /// </summary>
        /// <param name="provider">供应器。</param>
        /// <param name="level">缓存层级。</param>
        /// <returns></returns>
        public static void RegisterProvider(ICachingProvider provider, Level level = Level.First)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            switch (level)
            {
                case Level.First:
                case Level.Second:
                case Level.Third:
                    CacheProvider[level] = provider;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }


        /// <summary>
        /// 移除缓存供应器。
        /// </summary>
        /// <param name="level">缓存等级。</param>
        /// <returns></returns>
        public static bool RemoveProvider(Level level) => CacheProvider.Remove(level);

        /// <summary>
        /// 获取供应器。
        /// </summary>
        /// <param name="level">缓存等级。</param>
        /// <param name="provider">缓存供应器。</param>
        /// <returns></returns>
        public static bool TryGetProvider(Level level, out ICachingProvider provider) => CacheProvider.TryGetValue(level, out provider);

        /// <summary> 清空供应器。</summary>
        public static void Clear() => CacheProvider.Clear();

        /// <summary>
        /// 获取服务商的指定缓存服务。
        /// </summary>
        /// <param name="cacheName">缓存名称。</param>
        /// <param name="level">缓存等级。</param>
        /// <returns></returns>
        public static ICaching GetCache(string cacheName, Level level = Level.First)
        {
            ICachingProvider provider;

            switch (level)
            {
                case Level.First:
                case Level.Second:
                case Level.Third:
                    if (TryGetProvider(level, out provider))
                    {
                        return CacheDictionary.GetOrAdd(provider, _ => new ConcurrentDictionary<string, ICaching>())
                            .GetOrAdd(cacheName, name => provider.GetCache(name));
                    }
                    break;
                default:
                    var list = new List<ICaching>();

                    foreach (Level item in Enum.GetValues(typeof(Level)))
                    {
                        if ((level & item) == item)
                        {
                            if (TryGetProvider(item, out provider))
                            {
                                list.Add(CacheDictionary.GetOrAdd(provider, _ => new ConcurrentDictionary<string, ICaching>())
                                    .GetOrAdd(cacheName, name => provider.GetCache(name)));
                            }
                        }
                    }

                    if (list.Count > 0)
                    {
                        return new MultiCaching(list);
                    }

                    break;
            }

            throw new BusiException($"未配置【{level.GetText()}】的缓存供应器!");
        }
    }
}

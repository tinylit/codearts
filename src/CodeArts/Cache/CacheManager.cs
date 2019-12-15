using CodeArts.Exceptions;
using System;
using System.Collections.Concurrent;

namespace CodeArts.Cache
{
    /// <summary>
    /// 缓存管理
    /// </summary>
    public static class CacheManager
    {
        /// <summary>
        /// 缓存供应商
        /// </summary>
        private static readonly ConcurrentDictionary<CacheLevel, ICacheProvider> CacheProvider;

        /// <summary>
        /// 缓存集合
        /// </summary>
        private static readonly ConcurrentDictionary<ICacheProvider, ConcurrentDictionary<string, ICache>> CacheDictionary;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static CacheManager()
        {
            CacheProvider = new ConcurrentDictionary<CacheLevel, ICacheProvider>();
            CacheDictionary = new ConcurrentDictionary<ICacheProvider, ConcurrentDictionary<string, ICache>>();
        }

        /// <summary>
        /// 设置缓存供应商
        /// </summary>
        /// <param name="provider">供应商</param>
        /// <param name="level">缓存层级</param>
        /// <returns></returns>
        public static bool TryAddProvider(ICacheProvider provider, CacheLevel level = CacheLevel.First) => CacheProvider.TryAdd(level, provider);

        /// <summary>
        /// 移除缓存供应商
        /// </summary>
        /// <param name="level">缓存等级</param>
        /// <returns></returns>
        public static bool TryRemoveProvider(CacheLevel level) => CacheProvider.TryRemove(level, out _);

        /// <summary>
        /// 获取服务商
        /// </summary>
        /// <param name="level">缓存等级</param>
        /// <param name="provider">缓存供应器</param>
        /// <returns></returns>
        public static bool TryGetProvider(CacheLevel level, out ICacheProvider provider) => CacheProvider.TryGetValue(level, out provider);

        /// <summary> 清空适配器 </summary>
        public static void ClearAdapter() => CacheProvider.Clear();

        /// <summary>
        /// 获取服务商的指定缓存服务
        /// </summary>
        /// <param name="cacheName">缓存名称</param>
        /// <param name="level">缓存等级</param>
        /// <returns></returns>
        public static ICache GetCache(string cacheName, CacheLevel level = CacheLevel.First)
        {
            if (TryGetProvider(level, out ICacheProvider provider))
            {
                return CacheDictionary.GetOrAdd(provider, _ => new ConcurrentDictionary<string, ICache>())
                    .GetOrAdd(cacheName, name => provider.GetCache(name));
            }

            throw new BusiException($"未配置【{level.GetText()}】的缓存供应器!");
        }
    }
}

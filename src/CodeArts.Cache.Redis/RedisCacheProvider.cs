using System.Collections.Concurrent;

namespace CodeArts.Cache
{
    /// <summary>
    /// redis 缓存供应器。
    /// </summary>
    public class RedisCacheProvider : ICacheProvider
    {
        private static readonly ConcurrentDictionary<string, ICache> Caches;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static RedisCacheProvider() => Caches = new ConcurrentDictionary<string, ICache>();

        /// <summary>
        /// 获取缓存对象
        /// </summary>
        /// <param name="regionName">缓存区域名称</param>
        /// <returns></returns>
        public ICache GetCache(string regionName) => Caches.GetOrAdd(regionName, name => new RedisCache(name));
    }
}

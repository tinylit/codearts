using SkyBuilding.Cache;
using System.Collections.Concurrent;

namespace SkyBuilding.Mvc.Cache
{
    /// <summary>
    /// 内存缓存服务提供商
    /// </summary>
    public class RuntimeCacheProvider : ICacheProvider
    {
        /// <summary>
        /// 缓存对象
        /// </summary>
        private static readonly ConcurrentDictionary<string, ICache> Caches;

        static RuntimeCacheProvider() => Caches = new ConcurrentDictionary<string, ICache>();

        /// <summary>
        /// 获取 缓存是否可用
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary> 获取缓存对象 </summary>
        /// <param name="name">缓存名称</param>
        /// <returns></returns>
        public ICache GetCache(string name) => Caches.GetOrAdd(name, _ => new RuntimeCache(name));
    }
}

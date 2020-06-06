using CodeArts.Cache;
using System.Collections.Concurrent;

namespace CodeArts.Cache
{
    /// <summary>
    /// 内存缓存服务提供商
    /// </summary>
    public class RuntimeCacheProvider : ICacheProvider
    {
        /// <summary>
        /// 缓存对象
        /// </summary>
        private readonly ConcurrentDictionary<string, ICache> Caches;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RuntimeCacheProvider() => Caches = new ConcurrentDictionary<string, ICache>();

        /// <summary>
        /// 获取 缓存是否可用
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary> 获取缓存对象 </summary>
        /// <param name="regionName">缓存名称</param>
        /// <returns></returns>
        public ICache GetCache(string regionName)
        {
            if (regionName is null)
            {
                throw new System.ArgumentNullException(nameof(regionName));
            }

            if (string.IsNullOrWhiteSpace(regionName))
            {
                throw new System.ArgumentException("参数不能为空或空格字符!", nameof(regionName));
            }

            return Caches.GetOrAdd(regionName, name => new RuntimeCache(name));
        }
    }
}

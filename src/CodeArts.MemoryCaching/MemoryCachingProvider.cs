using System.Collections.Concurrent;

namespace CodeArts.Caching
{
    /// <summary>
    /// 内存缓存服务提供商。
    /// </summary>
    public class MemoryCachingProvider : ICachingProvider
    {
        /// <summary>
        /// 缓存对象。
        /// </summary>
        private readonly ConcurrentDictionary<string, ICaching> Caches;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public MemoryCachingProvider() => Caches = new ConcurrentDictionary<string, ICaching>();

        /// <summary> 获取缓存对象。</summary>
        /// <param name="regionName">缓存名称。</param>
        /// <returns></returns>
        public ICaching GetCache(string regionName)
        {
            if (regionName is null)
            {
                throw new System.ArgumentNullException(nameof(regionName));
            }

            if (string.IsNullOrWhiteSpace(regionName))
            {
                throw new System.ArgumentException("参数不能为空或空格字符!", nameof(regionName));
            }

            return Caches.GetOrAdd(regionName, name => new MemoryCaching(name));
        }
    }
}

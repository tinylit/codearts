using System;
using System.Collections.Concurrent;

namespace CodeArts.Caching
{
    /// <summary>
    /// redis 缓存供应器。
    /// </summary>
    public class RedisCachingProvider : ICachingProvider
    {
        private readonly int defaultDb;
        private readonly string connectString;
        private static readonly ConcurrentDictionary<string, ICaching> RedisCaches;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static RedisCachingProvider() => RedisCaches = new ConcurrentDictionary<string, ICaching>();

        /// <summary>
        /// 构造函数（获取配置中名为“redis”的值）。
        /// </summary>
        /// <param name="defaultDb">获取数据库的ID。</param>
        public RedisCachingProvider(int defaultDb = 0) : this("redis".Config<string>(), defaultDb)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connectString">链接。</param>
        /// <param name="defaultDb">获取数据库的ID。</param>
        public RedisCachingProvider(string connectString, int defaultDb = 0)
        {
            this.connectString = connectString ?? throw new ArgumentNullException(nameof(connectString));
            this.defaultDb = defaultDb;
        }

        /// <summary>
        /// 获取缓存对象。
        /// </summary>
        /// <param name="regionName">缓存区域名称。</param>
        /// <returns></returns>
        public ICaching GetCache(string regionName)
        {
            if (regionName is null)
            {
                throw new ArgumentNullException(nameof(regionName));
            }

            if (string.IsNullOrWhiteSpace(regionName))
            {
                throw new ArgumentException("参数不能为空或空格字符!", nameof(regionName));
            }

            return RedisCaches.GetOrAdd(regionName, name => new RedisCaching(name, connectString, defaultDb));
        }
    }
}

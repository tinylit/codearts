using System;
using System.Collections.Concurrent;

namespace CodeArts.Cache
{
    /// <summary>
    /// redis 缓存供应器。
    /// </summary>
    public class RedisCacheProvider : ICacheProvider
    {
        private readonly int defaultDb;
        private readonly string connectString;
        private static readonly ConcurrentDictionary<string, ICache> Caches;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static RedisCacheProvider() => Caches = new ConcurrentDictionary<string, ICache>();

        /// <summary>
        /// 构造函数（获取配置中名为“redis”的值）
        /// </summary>
        /// <param name="defaultDb">获取数据库的ID。</param>
        public RedisCacheProvider(int defaultDb = 0) : this("redis".Config<string>(), defaultDb)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectString">链接</param>
        /// <param name="defaultDb">获取数据库的ID。</param>
        public RedisCacheProvider(string connectString, int defaultDb = 0)
        {
            this.connectString = connectString ?? throw new ArgumentNullException(nameof(connectString));
            this.defaultDb = defaultDb;
        }

        /// <summary>
        /// 获取缓存对象
        /// </summary>
        /// <param name="regionName">缓存区域名称</param>
        /// <returns></returns>
        public ICache GetCache(string regionName) => Caches.GetOrAdd(regionName, name => new RedisCache(name, connectString, defaultDb));
    }
}

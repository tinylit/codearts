#if NETSTANDARD2_0_OR_GREATER
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace CodeArts.Caching
{
    /// <summary>
    /// 运行时内存管理。
    /// </summary>
    public class MemoryManager : Singleton<MemoryManager>
    {
        private MemoryManager() => _connections = new ConcurrentDictionary<string, IMemoryCache>();

        /// <summary>
        /// 内存管理对象。
        /// </summary>
        private readonly ConcurrentDictionary<string, IMemoryCache> _connections;

        /// <summary>
        /// 获取内存缓存。
        /// </summary>
        /// <param name="name">缓存名称。</param>
        /// <param name="sizeLimit">缓存大小。</param>
        /// <returns></returns>
        public IMemoryCache GetDatabase(string name = "default", int? sizeLimit = null) => _connections.GetOrAdd(name, new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions { SizeLimit = sizeLimit })));

        /// <summary>
        /// 删除内存缓存。
        /// </summary>
        /// <param name="name">缓存名称。</param>
        /// <returns></returns>
        public bool TryRemoveDataBase(string name)
        {
            if(_connections.TryRemove(name, out IMemoryCache memoryCache))
            {
                memoryCache.Dispose();

                return true;
            }

            return false;
        }
    }
}
#else
using System.Collections.Concurrent;
using System.Runtime.Caching;

namespace CodeArts.Caching
{
    /// <summary>
    /// 运行时内存管理。
    /// </summary>
    public class MemoryManager : Singleton<MemoryManager>
    {
        private MemoryManager() => _connections = new ConcurrentDictionary<string, MemoryCache>();

        /// <summary>
        /// 内存管理对象。
        /// </summary>
        private readonly ConcurrentDictionary<string, MemoryCache> _connections;

        /// <summary>
        /// 获取内存缓存。
        /// </summary>
        /// <param name="name">缓存名称。</param>
        /// <returns></returns>
        public MemoryCache GetDatabase(string name = "default") => _connections.GetOrAdd(name ?? "default", _ => new MemoryCache(name));

        /// <summary>
        /// 删除内存缓存。
        /// </summary>
        /// <param name="name">缓存名称。</param>
        /// <returns></returns>
        public bool TryRemoveDataBase(string name)
        {
            if (_connections.TryRemove(name, out MemoryCache memoryCache))
            {
                memoryCache.Dispose();

                return true;
            }

            return false;
        }
    }
}
#endif
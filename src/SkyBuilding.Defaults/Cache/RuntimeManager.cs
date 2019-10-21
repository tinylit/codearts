#if NETSTANDARD2_0 || NETSTANDARD2_1
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace SkyBuilding.Cache
{
    /// <summary>
    /// 运行时内存管理
    /// </summary>
    public class RuntimeMemoryManager : DesignMode.Singleton<RuntimeMemoryManager>
    {
        private RuntimeMemoryManager() => _connections = new ConcurrentDictionary<string, IMemoryCache>();

        /// <summary>
        /// 内存管理对象
        /// </summary>
        private readonly ConcurrentDictionary<string, IMemoryCache> _connections;

        /// <summary>
        /// 获取内存缓存
        /// </summary>
        /// <param name="name">缓存名称</param>
        /// <param name="sizeLimit">缓存大小</param>
        /// <returns></returns>
        public IMemoryCache GetDatabase(string name = "default", int? sizeLimit = null) => _connections.GetOrAdd(name, new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions { SizeLimit = sizeLimit })));

        /// <summary>
        /// 删除内存缓存
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void RemoveDataBase(string name) => _connections.TryRemove(name, out _);
    }
}
#else
using System.Collections.Concurrent;
using System.Runtime.Caching;

namespace SkyBuilding.Cache
{
    /// <summary>
    /// 运行时内存管理
    /// </summary>
    public class RuntimeManager : DesignMode.Singleton<RuntimeManager>
    {
        private RuntimeManager() => _connections = new ConcurrentDictionary<string, MemoryCache>();

        /// <summary>
        /// 内存管理对象
        /// </summary>
        private readonly ConcurrentDictionary<string, MemoryCache> _connections;

        /// <summary>
        /// 获取内存缓存
        /// </summary>
        /// <param name="name">缓存名称</param>
        /// <param name="sizeLimit">缓存大小</param>
        /// <returns></returns>
        public MemoryCache GetDatabase(string name = "default") => _connections.GetOrAdd(name ?? "default", _ => new MemoryCache(name));

        /// <summary>
        /// 删除内存缓存
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public void RemoveDataBase(string name) => _connections.TryRemove(name, out _);
    }
}
#endif
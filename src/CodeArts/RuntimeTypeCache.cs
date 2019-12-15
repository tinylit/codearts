using CodeArts.Runtime;
using System;
using System.Collections.Concurrent;

namespace CodeArts
{
    /// <summary>
    /// 类型缓存
    /// </summary>
    public sealed class RuntimeTypeCache : DesignMode.Singleton<RuntimeTypeCache>
    {
        private readonly static ConcurrentDictionary<RuntimeTypeHandle, TypeStoreItem> Cache = new ConcurrentDictionary<RuntimeTypeHandle, TypeStoreItem>();

        /// <summary>
        /// 私有构造函数
        /// </summary>
        private RuntimeTypeCache() { }

        /// <summary>
        /// 获取类型缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TypeStoreItem GetCache<T>() => GetCache(typeof(T));

        /// <summary>
        /// 获取类型缓存
        /// </summary>
        /// <returns></returns>
        public TypeStoreItem GetCache(Type type) => Cache.GetOrAdd(type.TypeHandle, _ => new TypeStoreItem(type));
    }
}

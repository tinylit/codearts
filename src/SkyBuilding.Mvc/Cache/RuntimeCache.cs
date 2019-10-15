#if NETSTANDARD2_0 || NETSTANDARD2_1
using Microsoft.Extensions.Caching.Memory;
#else
using System.Runtime.Caching;
#endif
using SkyBuilding.Cache;
using System;
using System.Collections.Generic;

namespace SkyBuilding.Mvc.Cache
{
    /// <summary>
    /// 内存缓存
    /// </summary>
    public class RuntimeCache : BaseCache
    {
#if NETSTANDARD2_0|| NETSTANDARD2_1
        private readonly int? _sizeLimit;
        private static IMemoryCache _database;
#else
        private static MemoryCache _database;
#endif
        private readonly string _name;
        private readonly string _region;

        /// <summary> 缓存区域 </summary>
        public override string Region => _region;

#if NETSTANDARD2_0|| NETSTANDARD2_1
        /// <summary>
        /// 内存缓存服务器
        /// </summary>
        /// <param name="name">内存块名</param>
        /// <param name="sizeLimit">缓存大小</param>
        public RuntimeCache(string name = "default", int? sizeLimit = null)
        {
            _region = _name = name;
            _sizeLimit = sizeLimit;

            _database = RuntimeMemoryManager.Instance.GetDatabase(_name, _sizeLimit);
        }
#else
        /// <summary>
        /// 内存缓存服务器
        /// </summary>
        /// <param name="name">内存块名</param>
        /// <param name="sizeLimit">缓存大小</param>
        public RuntimeCache(string name = "default")
        {
            _region = _name = name;

            _database = RuntimeManager.Instance.GetDatabase(_name);

            _name = name;
        }
#endif



        /// <summary>
        /// 清除缓存
        /// </summary>
        /// <returns></returns>
        public override void Clear()
        {
            _database.Dispose();
#if NETSTANDARD2_0 || NETSTANDARD2_1
            _database = RuntimeMemoryManager.Instance.GetDatabase(_name, _sizeLimit);
#else
            _database = RuntimeManager.Instance.GetDatabase(_name);
#endif
        }

        /// <summary>
        /// 设置有效时间
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="span">有效时间</param>
        /// <returns></returns>
        public override void Expire(string key, TimeSpan span)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (span <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(span));

            var value = _database.Get(GetKey(key));
            if (value is null)
                throw new ArgumentOutOfRangeException(nameof(key));

            Set(key, value, span);
        }

        /// <summary>
        /// 获取键值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override object Get(string key) => _database.Get(GetKey(key));

#if NETSTANDARD2_0 || NETSTANDARD2_1
        public override T Get<T>(string key) => _database.Get<T>(GetKey(key));
#else
        /// <summary>
        /// 获取键值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public override T Get<T>(string key)
        {
            var value = _database.Get(GetKey(key));

            if (value is null) return default;

            return (T)value;
        }
#endif
        /// <summary>
        /// 清除制定键的缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override void Remove(string key) => _database.Remove(GetKey(key));

        /// <summary>
        /// 清除缓存
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public override void Remove(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                Remove(key);
            }
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public override void Set(string key, object value) => _database.SetEx(GetKey(key), value);

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="span">过期时间</param>
        /// <returns></returns>
        public override void Set(string key, object value, TimeSpan span) => _database.SetEx(GetKey(key), value, span);
    }
}

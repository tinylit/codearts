#if NETSTANDARD2_0 || NETSTANDARD2_1
using Microsoft.Extensions.Caching.Memory;
#else
using System.Runtime.Caching;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace CodeArts.Caching
{
    /// <summary>
    /// 内存缓存。
    /// </summary>
    public class MemoryCaching : BaseCaching
    {
        private static readonly ConcurrentDictionary<string, object> LockCache = new ConcurrentDictionary<string, object>();

#if NETSTANDARD2_0|| NETSTANDARD2_1
        private readonly int? _sizeLimit;
        private static IMemoryCache _database;
#else
        private static MemoryCache _database;
#endif
        private readonly string _name;
        private readonly string _region;

        /// <summary> 缓存区域。 </summary>
        public override string Region => _region;

#if NETSTANDARD2_0|| NETSTANDARD2_1
        /// <summary>
        /// 内存缓存服务器。
        /// </summary>
        /// <param name="name">内存块名。</param>
        /// <param name="sizeLimit">缓存大小。</param>
        public MemoryCaching(string name = "default", int? sizeLimit = null)
        {
            _region = _name = name;
            _sizeLimit = sizeLimit;

            _database = MemoryManager.Instance.GetDatabase(_name, _sizeLimit);
        }
#else
        /// <summary>
        /// 内存缓存服务器。
        /// </summary>
        /// <param name="name">内存块名。</param>
        public MemoryCaching(string name = "default")
        {
            _region = _name = name;

            _database = MemoryManager.Instance.GetDatabase(_name);
        }
#endif



        /// <summary>
        /// 清除缓存。
        /// </summary>
        /// <returns></returns>
        public override void Clear()
        {
            if (MemoryManager.Instance.TryRemoveDataBase(_name))
            {
#if NETSTANDARD2_0 || NETSTANDARD2_1
                _database = MemoryManager.Instance.GetDatabase(_name, _sizeLimit);
#else
                _database = MemoryManager.Instance.GetDatabase(_name);
#endif
            }
        }

        /// <summary>
        /// 指定键是否存在。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override bool IsExsits(string key)
        {
#if NETSTANDARD2_0 || NETSTANDARD2_1
            return _database.TryGetValue(GetKey(key), out _);
#else
            return _database.Contains(GetKey(key));
#endif
        }

        /// <summary>
        /// 设置有效时间。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="span">有效时间。</param>
        /// <returns></returns>
        public override bool Expire(string key, TimeSpan span)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (span <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            var value = _database.Get(GetKey(key));

            if (value is null)
            {
                throw new KeyNotFoundException(nameof(key));
            }

            return Set(key, value, span);
        }

        /// <summary>
        /// 获取键值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override object Get(string key) => _database.Get(GetKey(key));

#if NETSTANDARD2_0 || NETSTANDARD2_1
        /// <summary>
        /// 获取键值。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override T Get<T>(string key) => _database.Get<T>(GetKey(key));
#else
        /// <summary>
        /// 获取键值。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override T Get<T>(string key)
        {
            var value = _database.Get(GetKey(key));

            if (value is null)
            {
                return default;
            }

            return (T)value;
        }
#endif
        /// <summary>
        /// 清除制定键的缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override void Remove(string key) => _database.Remove(GetKey(key));

        /// <summary>
        /// 清除缓存。
        /// </summary>
        /// <param name="keys">键集合。</param>
        /// <returns></returns>
        public override void Remove(IEnumerable<string> keys)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            var list = keys
                    .Distinct()
                    .ToList();

            if (list.Count == 0)
            {
                return;
            }

            list.ForEach(Remove);
        }

        /// <summary>
        /// 设置缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <returns></returns>
        public override bool Set(string key, object value) => SetEx(_database, GetKey(key), value);

        /// <summary>
        /// 设置缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <param name="span">过期时间。</param>
        /// <returns></returns>
        public override bool Set(string key, object value, TimeSpan span) => SetEx(_database, GetKey(key), value, span);

        /// <summary>
        /// 加锁。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="timeSpan">锁定时间（默认：10s）。</param>
        public override bool TryEnter(string key, TimeSpan? timeSpan = null) => Monitor.TryEnter(LockCache.GetOrAdd(GetKey(key), _ => new object()), timeSpan ?? TimeSpan.FromSeconds(10D));

        /// <summary>
        /// 退出锁。
        /// </summary>
        /// <param name="key">键。</param>
        public override void Exit(string key) => Monitor.Exit(LockCache.GetOrAdd(GetKey(key), _ => new object()));

#if NETSTANDARD2_0 || NETSTANDARD2_1
        /// <summary>
        /// 设置缓存。
        /// </summary>
        /// <param name="cache">缓存对象。</param>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <param name="span">过期时间。</param>
        /// <returns></returns>
        private static bool SetEx(IMemoryCache cache, string key, object value, TimeSpan? span = null)
        {
            if (span.HasValue)
            {
                cache.Set(key, value, span.Value);
            }
            else
            {
                cache.Set(key, value);
            }

            return true;
        }
#else
        /// <summary>
        /// 设置缓存。
        /// </summary>
        /// <param name="cache">缓存对象。</param>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <param name="span">过期时间。</param>
        /// <returns></returns>
        private static bool SetEx(MemoryCache cache, string key, object value, TimeSpan? span = null)
        {
            if (span.HasValue)
            {
                return cache.Add(key, value, DateTimeOffset.Now.Add(span.Value));
            }

            return cache.Add(key, value, ObjectCache.InfiniteAbsoluteExpiration);
        }
#endif
    }
}

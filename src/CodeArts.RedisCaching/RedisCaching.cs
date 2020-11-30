using CodeArts.Serialize.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts.Caching
{
    /// <summary>
    /// Redis 缓存。
    /// </summary>
    public class RedisCaching : BaseCaching
    {
        private readonly string _region;
        private readonly IDatabase _database;
        /// <summary>
        /// 缓存区。
        /// </summary>
        public override string Region => _region;

        private readonly RedisValue _lockValue;

        /// <summary>
        /// Redis缓存服务器。
        /// </summary>
        /// <param name="region">缓存区域。</param>
        /// <param name="connectString">连接字符串（未指定时，从配置文件中读取名为“redis”的字符串作为链接）。</param>
        /// <param name="defaultDb">默认db。</param>
        public RedisCaching(string region, string connectString = null, int defaultDb = 0)
        {
            _region = region ?? throw new ArgumentNullException(nameof(region));

            _lockValue = new RedisValue(region);

            _database = RedisManager.Instance.GetDatabase(connectString ?? "redis".Config<string>(), defaultDb);
        }

        /// <summary>
        /// 清所有空键（谨慎操作，数据无价！）。
        /// </summary>
        public override void Clear()
        {
            var script = LuaScript.Prepare("return redis.call('KEYS', @keypattern)");

            var results = _database.ScriptEvaluate(script, new { @keypattern = GetKey("*") });

            if (!results.IsNull)
            {
                _database.KeyDelete((RedisKey[])results);
            }
        }

        /// <summary>
        /// 指定键是否存在。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override bool IsExsits(string key) => _database.KeyExists(GetKey(key));

        /// <summary>
        /// 设置键过期时间。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="span">有效期。</param>
        public override bool Expire(string key, TimeSpan span)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _database.KeyExpire(GetKey(key), span);
        }

        /// <summary>
        /// 获取键值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override object Get(string key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var value = _database.StringGet(GetKey(key));

            if (value.HasValue)
            {
                return value.ToString();
            }

            return null;
        }
        /// <summary>
        /// 获取键值，并将结果转为指定类型。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public override T Get<T>(string key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var value = _database.StringGet(GetKey(key));

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            var type = typeof(T);

            string valueStr = value.ToString();

            if (type == typeof(string) || type == typeof(object))
            {
                return (T)(object)valueStr;
            }

            return type.IsValueType ? Mapper.Cast<T>(valueStr) : JsonHelper.Json<T>(valueStr);

        }

        /// <summary>
        /// 移除指定键的值。
        /// </summary>
        /// <param name="key">键。</param>
        public override void Remove(string key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _database.KeyDelete(GetKey(key));
        }

        /// <summary>
        /// 批量移除键的值。
        /// </summary>
        /// <param name="keys">键集合。</param>
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

            _database.KeyDelete(list.Select(key => new RedisKey(GetKey(key))).ToArray());
        }

        /// <summary>
        /// 设置值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        public override bool Set(string key, object value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value is null)
            {
                return _database.StringSet(GetKey(key), RedisValue.Null);
            }
            var type = value.GetType();

            if (type == typeof(string) || type.IsValueType)
            {
                return _database.StringSet(GetKey(key), value.ToString());
            }

            return _database.StringSet(GetKey(key), JsonHelper.ToJson(value));
        }

        /// <summary>
        /// 设置值。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <param name="span">有效期。</param>
        public override bool Set(string key, object value, TimeSpan span)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value is null)
            {
                return _database.StringSet(GetKey(key), RedisValue.Null, span);
            }
            var type = value.GetType();

            if (type == typeof(string) || type.IsValueType)
            {
                return _database.StringSet(GetKey(key), value.ToString(), span);
            }

            return _database.StringSet(GetKey(key), JsonHelper.ToJson(value), span);
        }

        /// <summary>
        /// 加锁。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="timeSpan">锁定时间（默认：10s）。</param>
        public override bool TryEnter(string key, TimeSpan? timeSpan = null) => _database.LockTake(GetKey(key), _lockValue, timeSpan ?? TimeSpan.FromSeconds(10));

        /// <summary>
        /// 退出锁。
        /// </summary>
        /// <param name="key">键。</param>
        public override void Exit(string key) => _database.LockRelease(GetKey(key), _lockValue);
    }
}

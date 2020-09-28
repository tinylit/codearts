using CodeArts.Serialize.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts.Cache
{
    /// <summary>
    /// Redis 缓存
    /// </summary>
    public class RedisCache : BaseCache
    {
        private readonly string _region;
        private readonly IDatabase _database;
        /// <summary>
        /// 缓存区
        /// </summary>
        public override string Region => _region;

        /// <summary>
        /// Redis缓存服务器
        /// </summary>
        /// <param name="region">缓存区域</param>
        /// <param name="connectString">连接字符串（未指定时，从配置文件中读取名为“redis”的字符串作为链接）</param>
        /// <param name="defaultDb">默认db</param>
        public RedisCache(string region, string connectString = null, int defaultDb = 0)
        {
            _region = region ?? throw new ArgumentNullException(nameof(region));
            _database = RedisManager.Instance.GetDatabase(connectString ?? "redis".Config<string>(), defaultDb);
        }

        /// <summary>
        /// 清所有空键（数据无价，禁止清除）。
        /// </summary>
        public override void Clear() => throw new NotImplementedException();

        /// <summary>
        /// 设置键过期时间。
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="span">时间片段</param>
        public override void Expire(string key, TimeSpan span)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _database.KeyExpire(GetKey(key), span);
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
        /// <typeparam name="T">类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        public override T Get<T>(string key)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var value = _database.StringGet(GetKey(key));

            if (!value.HasValue)
            {
                return default;
            }

            var type = typeof(T);

            string valueStr = value;

            if (type == typeof(string) || type == typeof(object))
            {
                return (T)(object)valueStr;
            }

            if (valueStr.IsEmpty())
            {
                return default;
            }

            return type.IsValueType ? valueStr.CastTo<T>() : JsonHelper.Json<T>(valueStr);

        }

        /// <summary>
        /// 移除指定键的值。
        /// </summary>
        /// <param name="key">键</param>
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
        /// <param name="keys">键集合</param>
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

            var redisKeys = new RedisKey[list.Count];

            list.ForEach((key, index) =>
            {
                redisKeys[index] = GetKey(key);
            });

            _database.KeyDelete(redisKeys);
        }

        /// <summary>
        /// 设置值。
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public override void Set(string key, object value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value is null)
            {
                _database.StringSet(GetKey(key), RedisValue.Null);
            }
            else
            {
                var type = value.GetType();

                if (type == typeof(string) || type.IsValueType)
                {
                    _database.StringSet(GetKey(key), value.ToString());
                }
                else
                {
                    _database.StringSet(GetKey(key), JsonHelper.ToJson(value));
                }
            }
        }

        /// <summary>
        /// 设置值。
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="span">有效期</param>
        public override void Set(string key, object value, TimeSpan span)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value is null)
            {
                _database.StringSet(GetKey(key), RedisValue.Null, span);
            }
            else
            {
                var type = value.GetType();

                if (type == typeof(string) || type.IsValueType)
                {
                    _database.StringSet(GetKey(key), value.ToString(), span);
                }
                else
                {
                    _database.StringSet(GetKey(key), JsonHelper.ToJson(value), span);
                }
            }
        }
    }
}

#if NETSTANDARD2_0 || NETSTANDARD2_1
using Microsoft.Extensions.Caching.Memory;
#endif
using System;

namespace SkyBuilding.Mvc.Cache
{
    /// <summary>
    /// 内存扩展方法
    /// </summary>
    public static class RuntimeExtensions
    {
#if NETSTANDARD2_0 || NETSTANDARD2_1
        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="cache">缓存对象</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="span">过期时间</param>
        /// <returns></returns>
        public static void SetEx(this IMemoryCache cache, string key, object value, TimeSpan? span = null)
        {
            if (span.HasValue)
            {
                cache.Set(key, value, span.Value);
            }
            else
            {
                cache.Set(key, value);
            }
        }
#else
        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="cache">缓存对象</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="span">过期时间</param>
        /// <returns></returns>
        public static void SetEx(this System.Runtime.Caching.MemoryCache cache, string key, object value, TimeSpan? span = null)
        {
            if (span.HasValue)
            {
                cache.Set(key, value, new DateTimeOffset(DateTime.UtcNow.Add(span.Value), TimeSpan.Zero));
            }
            else
            {
                cache.Set(key, value, DateTimeOffset.MaxValue);
            }
        }
#endif
    }
}

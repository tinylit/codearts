using System;
using System.Collections.Generic;
#if NET_NORMAL || NETSTANDARD2_0
using System.Threading.Tasks;
#endif

namespace CodeArts.Caching
{
    /// <summary>
    /// 缓存接口。
    /// </summary>
    public interface ICaching
    {
        /// <summary>
        /// 设置缓存（无失效时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <returns></returns>
        bool Set(string key, object value);

        /// <summary>
        /// 设置缓存（有效期）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <param name="span">有效期。</param>
        /// <returns></returns>
        bool Set(string key, object value, TimeSpan span);

        /// <summary>
        /// 设置缓存（失效时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <param name="time">失效时间。</param>
        /// <returns></returns>
        bool Set(string key, object value, DateTime time);

        /// <summary>
        /// 指定键是否存在。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        bool IsExsits(string key);

        /// <summary>
        /// 设置缓存过期时间（有限时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="span">有效期。</param>
        /// <returns></returns>
        bool Expire(string key, TimeSpan span);

        /// <summary>
        /// 设置缓存过期时间（失效时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="time">失效时间。</param>
        /// <returns></returns>
        bool Expire(string key, DateTime time);

        /// <summary>
        /// 获取缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        object Get(string key);

        /// <summary>
        /// 获取缓存（泛型）。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="key">键。</param>
        /// <returns></returns>
        T Get<T>(string key);

        /// <summary>
        /// 清除指定缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        void Remove(string key);

        /// <summary>
        /// 批量清除缓存。
        /// </summary>
        /// <param name="keys">键集合。</param>
        /// <returns></returns>
        void Remove(IEnumerable<string> keys);

        /// <summary>
        /// 清空缓存。
        /// </summary>
        /// <returns></returns>
        void Clear();

        /// <summary>
        /// 加锁。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="timeSpan">锁定时间（默认：10s）。</param>
        bool TryEnter(string key, TimeSpan? timeSpan = null);

        /// <summary>
        /// 退出锁。
        /// </summary>
        /// <param name="key">键。</param>
        void Exit(string key);

#if NET_NORMAL || NETSTANDARD2_0
        /// <summary>
        /// 设置缓存（无失效时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <returns></returns>
        Task<bool> SetAsync(string key, object value);

        /// <summary>
        /// 设置缓存（有效期）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <param name="span">有效期。</param>
        /// <returns></returns>
        Task<bool> SetAsync(string key, object value, TimeSpan span);

        /// <summary>
        /// 设置缓存（失效时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <param name="time">失效时间。</param>
        /// <returns></returns>
        Task<bool> SetAsync(string key, object value, DateTime time);

        /// <summary>
        /// 指定键是否存在。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        Task<bool> IsExsitsAsync(string key);

        /// <summary>
        /// 设置缓存过期时间（有限时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="span">有效期。</param>
        /// <returns></returns>
        Task<bool> ExpireAsync(string key, TimeSpan span);

        /// <summary>
        /// 设置缓存过期时间（失效时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="time">失效时间。</param>
        /// <returns></returns>
        Task<bool> ExpireAsync(string key, DateTime time);

        /// <summary>
        /// 获取缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        Task<object> GetAsync(string key);

        /// <summary>
        /// 获取缓存（泛型）。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="key">键。</param>
        /// <returns></returns>
        Task<T> GetAsync<T>(string key);

        /// <summary>
        /// 清除指定缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        Task RemoveAsync(string key);

        /// <summary>
        /// 批量清除缓存。
        /// </summary>
        /// <param name="keys">键集合。</param>
        /// <returns></returns>
        Task RemoveAsync(IEnumerable<string> keys);

        /// <summary>
        /// 清空缓存。
        /// </summary>
        /// <returns></returns>
        Task ClearAsync();

        /// <summary>
        /// 加锁。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="timeSpan">锁定时间（默认：10s）。</param>
        Task<bool> TryEnterAsync(string key, TimeSpan? timeSpan = null);

        /// <summary>
        /// 退出锁。
        /// </summary>
        /// <param name="key">键。</param>
        Task ExitAsync(string key);
#endif
    }
}

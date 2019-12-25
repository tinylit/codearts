using System;
using System.Collections.Generic;

namespace CodeArts.Cache
{
    /// <summary>
    /// 缓存接口
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// 设置缓存（无失效时间）
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        void Set(string key, object value);

        /// <summary>
        /// 设置缓存（有效期）
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="span">有效期</param>
        /// <returns></returns>
        void Set(string key, object value, TimeSpan span);

        /// <summary>
        /// 设置缓存（失效时间）
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="time">失效时间</param>
        /// <returns></returns>
        void Set(string key, object value, DateTime time);

        /// <summary>
        /// 设置缓存过期时间（有限时间）
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="span">有效期</param>
        /// <returns></returns>
        void Expire(string key, TimeSpan span);

        /// <summary>
        /// 设置缓存过期时间（失效时间）
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="time">失效时间</param>
        /// <returns></returns>
        void Expire(string key, DateTime time);

        /// <summary>
        /// 获取缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        object Get(string key);

        /// <summary>
        /// 获取缓存（泛型）
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="key">键</param>
        /// <returns></returns>
        T Get<T>(string key);

        /// <summary>
        /// 清除指定缓存
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        void Remove(string key);

        /// <summary>
        /// 批量清除缓存
        /// </summary>
        /// <param name="keys">键集合</param>
        /// <returns></returns>
        void Remove(IEnumerable<string> keys);

        /// <summary>
        /// 清空缓存
        /// </summary>
        /// <returns></returns>
        void Clear();
    }
}

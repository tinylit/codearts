using System;
using System.Collections.Generic;

namespace CodeArts.Caching
{
    /// <summary>
    /// 缓存基类。
    /// </summary>
    public abstract class BaseCaching : ICaching
    {
        /// <summary> 缓存区域。 </summary>
        public abstract string Region { get; }

        /// <summary>
        /// 设置缓存（无失效时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <returns></returns>
        public abstract bool Set(string key, object value);

        /// <summary>
        /// 设置缓存（有效期）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <param name="span">有效期。</param>
        /// <returns></returns>
        public abstract bool Set(string key, object value, TimeSpan span);

        /// <summary>
        /// 设置缓存（失效时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        /// <param name="time">失效时间。</param>
        /// <returns></returns>
        public virtual bool Set(string key, object value, DateTime time) => Set(key, value, time - DateTime.Now);

        /// <summary>
        /// 指定键是否存在。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public abstract bool IsExsits(string key);

        /// <summary>
        /// 设置缓存过期时间（有限时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="span">有效期。</param>
        /// <returns></returns>
        public abstract bool Expire(string key, TimeSpan span);

        /// <summary>
        /// 设置缓存过期时间（失效时间）。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="time">失效时间。</param>
        /// <returns></returns>
        public virtual bool Expire(string key, DateTime time) => Expire(key, time - DateTime.Now);

        /// <summary>
        /// 获取缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public abstract object Get(string key);

        /// <summary>
        /// 获取缓存（泛型）。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public abstract T Get<T>(string key);

        /// <summary>
        /// 清除指定缓存。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public abstract void Remove(string key);

        /// <summary>
        /// 批量清除缓存。
        /// </summary>
        /// <param name="keys">键集合。</param>
        /// <returns></returns>
        public abstract void Remove(IEnumerable<string> keys);

        /// <summary>
        /// 清空缓存。
        /// </summary>
        /// <returns></returns>
        public abstract void Clear();

        /// <summary> 
        /// 获取缓存键。 
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        protected virtual string GetKey(string key) => string.Concat(Region, ":", key); //, "@", key.GetHashCode()

        /// <summary>
        /// 加锁。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="timeSpan">锁定时间（默认：10s）。</param>
        public abstract bool TryEnter(string key, TimeSpan? timeSpan = null);

        /// <summary>
        /// 退出锁。
        /// </summary>
        /// <param name="key">键。</param>
        public abstract void Exit(string key);
    }
}

using System;
using System.Diagnostics;

namespace CodeArts
{
    /// <summary>
    /// 主键
    /// </summary>
    [DebuggerDisplay("{Value}")]
    public abstract class Key : IEquatable<Key>, IEquatable<long>, IComparable<Key>, IComparable<long>
    {
        /// <summary>
        /// 主键
        /// </summary>
        /// <param name="value">键值</param>
        public Key(long value) => Value = value;

        /// <summary>
        /// 值
        /// </summary>
        public long Value { get; }

        /// <summary>
        /// 隐式转换为长整型
        /// </summary>
        /// <param name="key">主键</param>
        public static implicit operator long(Key key) => key?.Value ?? 0L;

        /// <summary>
        /// 长整型隐式转换为主键。
        /// </summary>
        /// <param name="value">键值</param>
        public static implicit operator Key(long value) => KeyGen.Create(value);

        /// <summary>
        /// 是否相等
        /// </summary>
        /// <param name="value">键值</param>
        /// <returns></returns>
        public bool Equals(long value) => Value == value;

        /// <summary>
        /// 是否相等
        /// </summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        public bool Equals(Key key) => key?.Value == Value;

        /// <summary>
        /// 比较
        /// </summary>
        /// <param name="value">键值</param>
        /// <returns></returns>
        public int CompareTo(long value) => Value.CompareTo(value);

        /// <summary>
        /// 比较
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int CompareTo(Key key) => Value.CompareTo(key?.Value ?? 0L);

        /// <summary>
        /// 对象的值转换为本地时间。
        /// </summary>
        /// <returns></returns>
        public DateTime ToLocalTime() => ToUniversalTime().ToLocalTime();

        /// <summary>
        /// 对象的值转换为协调世界时 (UTC)。
        /// </summary>
        /// <returns></returns>
        public abstract DateTime ToUniversalTime();
    }
}

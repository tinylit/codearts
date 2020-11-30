using System;
using System.Diagnostics;

namespace CodeArts
{
    /// <summary>
    /// 主键。
    /// </summary>
    [DebuggerDisplay("{Value}")]
    public abstract class Key : IEquatable<Key>, IEquatable<long>, IComparable<Key>, IComparable<long>
    {
        /// <summary>
        /// 主键。
        /// </summary>
        /// <param name="value">键值。</param>
        public Key(long value) => Value = value;

        /// <summary>
        /// 值。
        /// </summary>
        public long Value { get; }

        /// <summary>
        /// 工作机号。
        /// </summary>
        public abstract int WorkId { get; }

        /// <summary>
        /// 机房（数据中心）。
        /// </summary>
        public abstract int DataCenterId { get; }

        /// <summary>
        /// 隐式转换为长整型。
        /// </summary>
        /// <param name="key">主键。</param>
        public static implicit operator long(Key key) => key?.Value ?? 0L;

        /// <summary>
        /// 长整型隐式转换为主键。
        /// </summary>
        /// <param name="value">键值。</param>
        public static implicit operator Key(long value) => KeyGen.Create(value);

        /// <summary>
        /// 是否相等。
        /// </summary>
        /// <param name="value">键值。</param>
        /// <returns></returns>
        public bool Equals(long value) => Value == value;

        /// <summary>
        /// 是否相等。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns></returns>
        public bool Equals(Key key) => key?.Value == Value;

        /// <summary>
        /// 比较。
        /// </summary>
        /// <param name="value">键值。</param>
        /// <returns></returns>
        public int CompareTo(long value) => Value.CompareTo(value);

        /// <summary>
        /// 比较。
        /// </summary>
        /// <param name="key">键值。</param>
        /// <returns></returns>
        public int CompareTo(Key key) => Value.CompareTo(key?.Value ?? 0L);

        /// <summary>
        /// 主键生成的时刻。
        /// </summary>
        public DateTime At => ToLocalTime();

        /// <summary>
        /// 主键生成的时刻(Utc)。
        /// </summary>
        public DateTime UtcAt => ToUniversalTime();

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

        /// <summary>
        /// 转本地日期字符串。
        /// </summary>
        /// <returns></returns>
        public string ToLocalTimeString() => ToLocalTime().ToString();

        /// <summary>
        /// 转本地日期字符串。
        /// </summary>
        /// <param name="format">标准或自定义日期和时间格式字符串。</param>
        /// <returns></returns>
        public string ToLocalTimeString(string format) => ToLocalTime().ToString(format);

        /// <summary>
        /// 转UTC日期字符串。
        /// </summary>
        /// <returns></returns>
        public string ToUniversalTimeString() => ToUniversalTime().ToString();

        /// <summary>
        /// 转UTC日期字符串。
        /// </summary>
        /// <param name="format">标准或自定义日期和时间格式字符串。</param>
        /// <returns></returns>
        public string ToUniversalTimeString(string format) => ToUniversalTime().ToString(format);
    }
}

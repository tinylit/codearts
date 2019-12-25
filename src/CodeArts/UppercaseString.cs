using System;
using System.Diagnostics;

namespace CodeArts
{
    /// <summary>
    /// 大写字符串
    /// </summary>
    [DebuggerDisplay("{value}")]
    public struct UppercaseString : IEquatable<UppercaseString>, IEquatable<string>
    {
        private readonly string value;

        /// <summary>
        /// 空值
        /// </summary>

        public readonly static UppercaseString Empty = new UppercaseString();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="value">字符串</param>
        public UppercaseString(string value)
        {
            this.value = value?.ToUpper();
        }

        /// <summary>
        /// 与指定对象是否相同。
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is UppercaseString value)
            {
                return Equals(value);
            }
            if (obj is string text)
            {
                return Equals(text);
            }
            return false;
        }
        /// <summary>
        /// 哈希码
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// 与指定数据是否相同。
        /// </summary>
        /// <param name="other">数据</param>
        /// <returns></returns>
        public bool Equals(UppercaseString other) => value == other.value;

        /// <summary>
        /// 与指定数据是否相同。
        /// </summary>
        /// <param name="other">数据</param>
        /// <returns></returns>
        public bool Equals(string other) => other is null ? value == other : value == other.ToUpper();

        /// <summary>
        /// 对象比较
        /// </summary>
        /// <param name="left">对象1</param>
        /// <param name="right">对象2</param>
        /// <returns></returns>
        public static bool operator ==(UppercaseString left, UppercaseString right) => left.Equals(right);

        /// <summary>
        /// 对象比较
        /// </summary>
        /// <param name="left">对象1</param>
        /// <param name="right">对象2</param>
        /// <returns></returns>
        public static bool operator !=(UppercaseString left, UppercaseString right) => !left.Equals(right);

        /// <summary>
        /// 提供隐式转换
        /// </summary>
        /// <param name="value">值</param>
        public static implicit operator UppercaseString(string value) => new UppercaseString(value);

        /// <summary>
        /// 返回大写的字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString() => value;
    }
}

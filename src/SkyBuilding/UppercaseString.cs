using System;
using System.Diagnostics;

namespace SkyBuilding
{
    /// <summary>
    /// 大写字符串
    /// </summary>
    [DebuggerDisplay("{value}")]
    public struct UppercaseString : IEquatable<UppercaseString>, IEquatable<string>
    {
        private readonly string value;

        public readonly static UppercaseString Empty = new UppercaseString();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="value">字符串</param>
        public UppercaseString(string value)
        {
            this.value = value?.ToUpper();
        }

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

        public override int GetHashCode() => base.GetHashCode();

        public bool Equals(UppercaseString other) => value == other.value;

        public bool Equals(string other) => other is null ? value == other : value == other.ToUpper();

        public static bool operator ==(UppercaseString left, UppercaseString right) => left.Equals(right);

        public static bool operator !=(UppercaseString left, UppercaseString right) => !left.Equals(right);

        public static implicit operator UppercaseString(string value) => new UppercaseString(value);

        public override string ToString() => value;
    }
}

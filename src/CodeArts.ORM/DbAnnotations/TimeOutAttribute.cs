using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 超时时间
    /// </summary>
    public sealed class TimeOutAttribute : Attribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">超时时间</param>
        public TimeOutAttribute(int value)
        {
            Value = value;
        }
        /// <summary>
        /// 超时时间。
        /// </summary>
        public int Value { get; }
    }
}

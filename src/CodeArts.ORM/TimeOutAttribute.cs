using System;

namespace CodeArts.ORM
{
    /// <summary>
    /// 超时时间
    /// </summary>
    public sealed class TimeOutAttribute : Attribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="timeOut">超时时间</param>
        public TimeOutAttribute(int timeOut)
        {
            TimeOut = timeOut;
        }
        /// <summary>
        /// 超时时间。
        /// </summary>
        public int TimeOut { get; }
    }
}

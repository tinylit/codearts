using System;

namespace CodeArts.ORM.Exceptions
{
    /// <summary>
    /// 不支持
    /// </summary>
    public class DNotSupportedException : NotSupportedException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DNotSupportedException()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">异常消息</param>
        public DNotSupportedException(string message) : base(message)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="innerException">引发错误的异常</param>
        public DNotSupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

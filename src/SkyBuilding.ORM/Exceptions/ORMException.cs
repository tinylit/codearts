using System;

namespace SkyBuilding.ORM.Exceptions
{
    /// <summary>
    /// ORM异常
    /// </summary>
    public class ORMException : Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ORMException()
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误消息</param>
        public ORMException(string message) : base(message)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">引发的异常</param>
        public ORMException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

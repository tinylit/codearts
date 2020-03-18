using System;

namespace CodeArts.ORM.Exceptions
{
    /// <summary>
    /// 未实现主键异常
    /// </summary>
    public class NotImplementedKeyException : DException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public NotImplementedKeyException() : base()
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        public NotImplementedKeyException(string message) : base(message)
        {
        }
    }
}

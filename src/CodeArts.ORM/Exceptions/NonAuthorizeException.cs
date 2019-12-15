using System;

namespace CodeArts.ORM.Exceptions
{
    /// <summary>
    /// 未授权的异常
    /// </summary>
    public class NonAuthorizeException : NotSupportedException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public NonAuthorizeException()
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        public NonAuthorizeException(string message) : base(message)
        {
        }
    }
}

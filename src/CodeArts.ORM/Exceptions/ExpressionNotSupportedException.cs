using System;

namespace CodeArts.ORM.Exceptions
{
    /// <summary>
    /// 表达式访问器不支持异常
    /// </summary>
    public class ExpressionNotSupportedException : NotSupportedException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ExpressionNotSupportedException()
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        public ExpressionNotSupportedException(string message) : base(message)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        /// <param name="innerException">引发异常</param>
        public ExpressionNotSupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

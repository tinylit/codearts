using System;

namespace CodeArts.Emit
{
    /// <summary>
    /// 表达式异常。
    /// </summary>
    public class AstException : Exception
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AstException()
        {
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="message">异常信息</param>
        public AstException(string message) : base(message)
        {
        }
    }
}

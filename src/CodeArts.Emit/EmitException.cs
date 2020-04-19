using System;

namespace CodeArts.Emit
{
    /// <summary>
    /// 表达式异常。
    /// </summary>
    public class EmitException : Exception
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public EmitException()
        {
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="message">异常信息</param>
        public EmitException(string message) : base(message)
        {
        }
    }
}

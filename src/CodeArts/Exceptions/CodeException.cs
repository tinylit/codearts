using System;

namespace CodeArts.Exceptions
{
    /// <summary>
    /// 异常
    /// </summary>
    public class CodeException : Exception
    {
        /// <summary>
        /// 错误码
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// 异常
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="errorCode">错误</param>
        public CodeException(string message, int errorCode = StatusCodes.Error)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}

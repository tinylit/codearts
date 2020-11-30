using System;

namespace CodeArts.Exceptions
{
    /// <summary>
    /// 异常。
    /// </summary>
    public class CodeException : Exception
    {
        /// <summary>
        /// 错误码。
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// 异常。
        /// </summary>
        /// <param name="message">异常消息。</param>
        /// <param name="errorCode">错误编码。</param>
        public CodeException(string message, int errorCode = StatusCodes.Error)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 异常。
        /// </summary>
        /// <param name="message">异常消息。</param>
        /// <param name="innerException">引发异常的异常。</param>
        /// <param name="errorCode">错误编码。</param>
        public CodeException(string message, Exception innerException, int errorCode = StatusCodes.Error) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}

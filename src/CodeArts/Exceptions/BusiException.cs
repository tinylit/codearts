using System;

namespace CodeArts.Exceptions
{
    /// <summary> 业务执行中，业务逻辑不满足返回异常 </summary>
    public class BusiException : CodeException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="errorCode">状态编码</param>
        public BusiException(string message, int errorCode = StatusCodes.BusiError) : base(message, errorCode)
        {
        }

        /// <summary>
        /// 异常。
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="innerException">引发异常的异常</param>
        /// <param name="errorCode">错误编码</param>
        public BusiException(string message, Exception innerException, int errorCode = StatusCodes.Error) : base(message, innerException, errorCode)
        {
        }
    }
}

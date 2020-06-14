using System;
using System.Data.Common;

namespace CodeArts.ORM.Exceptions
{
    /// <summary>
    /// 数据异常
    /// </summary>
    public class DException : DbException
    {
        /// <summary>
        /// 错误码
        /// </summary>
        public override int ErrorCode { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="errorCode">错误编码</param>
        public DException(int errorCode = StatusCodes.DbError)
        {
            ErrorCode = errorCode;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        /// <param name="errorCode">错误编码</param>
        public DException(string message, int errorCode = StatusCodes.DbError) : base(message)
        {
             ErrorCode = errorCode;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        /// <param name="innerException">引发错误的异常</param>
        /// <param name="errorCode">错误编码</param>
        public DException(string message, Exception innerException, int errorCode = StatusCodes.DbError) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}

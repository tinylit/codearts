using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyBuilding.Exceptions
{
    /// <summary>
    /// 异常
    /// </summary>
    public class SkyException : Exception
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
        public SkyException(string message, int errorCode = StatusCodes.Error)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}

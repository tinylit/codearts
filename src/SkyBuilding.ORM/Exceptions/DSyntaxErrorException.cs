using System;
using System.Data;

namespace SkyBuilding.ORM.Exceptions
{
    /// <summary>
    /// 执行语句语法错误
    /// </summary>
    public class DSyntaxErrorException : SyntaxErrorException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DSyntaxErrorException()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        public DSyntaxErrorException(string message) : base(message)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        /// <param name="innerException">引发错误的异常</param>
        public DSyntaxErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

using System;
using System.Data.Common;

namespace SkyBuilding.ORM.Exceptions
{
    /// <summary>
    /// 数据异常
    /// </summary>
    public class DException : DbException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DException()
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        public DException(string message) : base(message)
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        /// <param name="innerException">引发错误的异常</param>
        public DException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

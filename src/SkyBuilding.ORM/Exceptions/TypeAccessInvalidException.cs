using System;
using System.Runtime.Serialization;

namespace SkyBuilding.ORM.Exceptions
{
    /// <summary>
    /// 访问类型无效
    /// </summary>
    [Serializable]
    public class TypeAccessInvalidException : TypeAccessException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public TypeAccessInvalidException()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误消息</param>
        public TypeAccessInvalidException(string message) : base(message)
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">引发的异常</param>
        public TypeAccessInvalidException(string message, Exception innerException) : base(message, innerException)
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">数据流上下文</param>
        protected TypeAccessInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
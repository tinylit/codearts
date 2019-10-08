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
        public TypeAccessInvalidException()
        {
        }

        public TypeAccessInvalidException(string message) : base(message)
        {
        }

        public TypeAccessInvalidException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TypeAccessInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
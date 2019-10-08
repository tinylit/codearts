using System;

namespace SkyBuilding.ORM.Exceptions
{
    /// <summary>
    /// ORM异常
    /// </summary>
    public class ORMException : Exception
    {
        public ORMException()
        {
        }

        public ORMException(string message) : base(message)
        {
        }

        public ORMException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

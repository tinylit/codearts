using System;

namespace CodeArts.Db.Exceptions
{
    /// <summary>
    /// 未授权的异常。
    /// </summary>
    public class NonAuthorizedException : InvalidOperationException
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public NonAuthorizedException()
        {
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="message">错误信息。</param>
        public NonAuthorizedException(string message) : base(message)
        {
        }
    }
}

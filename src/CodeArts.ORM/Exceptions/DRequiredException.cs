using CodeArts.Exceptions;

namespace CodeArts.ORM.Exceptions
{
    /// <summary>
    /// 数据必填异常
    /// </summary>
    public class DRequiredException : CodeException
    {
        private const string DefaultError = "未查询到满足指定条件的相关信息！";
        /// <summary>
        /// 构造函数
        /// </summary>
        public DRequiredException() : base(DefaultError)
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        public DRequiredException(string message) : base(message ?? DefaultError)
        {
        }
    }
}

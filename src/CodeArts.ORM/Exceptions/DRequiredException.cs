namespace CodeArts.ORM.Exceptions
{
    /// <summary>
    /// 数据必填异常
    /// </summary>
    public class DRequiredException : ORMException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DRequiredException()
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        public DRequiredException(string message) : base(message)
        {
        }
    }
}

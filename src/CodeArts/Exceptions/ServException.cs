namespace CodeArts.Exceptions
{
    /// <summary>
    /// 各服务层异常
    /// </summary>
    public class ServException : SkyException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误编码</param>
        public ServException(string message, int errorCode = StatusCodes.ServError) : base(message, errorCode)
        {
        }
    }
}

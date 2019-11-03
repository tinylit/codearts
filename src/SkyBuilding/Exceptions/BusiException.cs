namespace SkyBuilding.Exceptions
{
    /// <summary> 业务执行中，业务逻辑不满足返回异常 </summary>
    public class BusiException : SkyException
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">异常消息</param>
        /// <param name="errorCode">状态编码</param>
        public BusiException(string message, int errorCode = StatusCodes.BusiError) : base(message, errorCode)
        {
        }
    }
}

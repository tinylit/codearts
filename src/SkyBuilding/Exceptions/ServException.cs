namespace SkyBuilding.Exceptions
{
    /// <summary>
    /// 各服务层异常
    /// </summary>
    public class ServException : SkyException
    {
        public ServException(string message, int errorCode = StatusCodes.ServError) : base(message, errorCode)
        {
        }
    }
}

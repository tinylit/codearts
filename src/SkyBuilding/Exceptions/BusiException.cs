namespace SkyBuilding.Exceptions
{
    /// <summary> 业务执行中，业务逻辑不满足返回异常 </summary>
    public class BusiException : SkyException
    {
        public BusiException(string message, int errorCode = StatusCodes.BusiError) : base(message, errorCode)
        {
        }
    }
}

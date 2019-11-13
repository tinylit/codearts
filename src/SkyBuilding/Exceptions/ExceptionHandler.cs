using SkyBuilding.Log;
using System;
using System.Net;

namespace SkyBuilding.Exceptions
{
    /// <summary> 异常处理类 </summary>
    public static class ExceptionHandler
    {
        /// <summary> 日志 </summary>
        private static readonly ILogger Logger = LogManager.Logger(typeof(ExceptionHandler));

        /// <summary> 异常事件 </summary>
        public static event Action<Exception> OnException;

        /// <summary> 服务异常事件 </summary>
        public static event Action<SkyException> OnSkyException;

        /// <summary> 服务异常事件 </summary>
        public static event Action<ServException> OnServException;

        /// <summary> 业务异常事件 </summary>
        public static event Action<BusiException> OnBusiException;

        /// <summary> 异常处理 </summary>
        /// <param name="exception">异常信息</param>
        public static DResult Handler(Exception exception)
        {
            var error = exception.GetBaseException();
            switch (error)
            {
                case SkyException sky:

                    switch (sky)
                    {
                        case ServException serv:
                            OnServException?.Invoke(serv);
                            break;
                        case BusiException busi:
                            OnBusiException?.Invoke(busi);
                            break;
                        default:
                            OnSkyException?.Invoke(sky);
                            break;
                    }

                    return DResult.Error(sky.Message, sky.ErrorCode);
                case OperationCanceledException _:
                    //操作取消
                    return null;
                default:
                    OnException?.Invoke(error);

                    var type = error.GetType();

                    if (type.Name == "ValidationException")
                    {
                        return DResult.Error(error.Message);
                    }

                    Logger.Error(error.Message, error);

                    if (error is WebException web)
                    {
                        string msg = $"接口({web.Response?.ResponseUri?.ToString()}-{web.Status})访问失败!";
                        switch (web.Status)
                        {
                            case WebExceptionStatus.RequestCanceled:
                                return null;
                            case WebExceptionStatus.NameResolutionFailure:
                                return DResult.Error(msg, StatusCodes.BadRequest);
                            case WebExceptionStatus.Timeout:
                            case WebExceptionStatus.ConnectFailure:
                                return DResult.Error(msg, StatusCodes.RequestTimeout);
                            case WebExceptionStatus.MessageLengthLimitExceeded:
                                return DResult.Error(msg, StatusCodes.RequestEntityTooLarge);
                            case WebExceptionStatus.SendFailure:
                            case WebExceptionStatus.PipelineFailure:
                            case WebExceptionStatus.ConnectionClosed:
                                return DResult.Error(msg, StatusCodes.InternalServerError);
                            case WebExceptionStatus.ProtocolError:
                                return DResult.Error(msg, StatusCodes.Unauthorized);
                            case WebExceptionStatus.TrustFailure:
                                return DResult.Error(msg, StatusCodes.Forbidden);
                            case WebExceptionStatus.SecureChannelFailure:
                            case WebExceptionStatus.ServerProtocolViolation:
                                return DResult.Error(msg, StatusCodes.HttpVersionNotSupported);
                            case WebExceptionStatus.ReceiveFailure:
                            case WebExceptionStatus.KeepAliveFailure:
                                return DResult.Error(msg, StatusCodes.GatewayTimeout);
                            case WebExceptionStatus.ProxyNameResolutionFailure:
                                return DResult.Error(msg, StatusCodes.UseProxy);
                            default:
                                return DResult.Error(msg, StatusCodes.Error);
                        }
                    }

                    if (error is TimeoutException)
                    {
                        return StatusCodes.TimeOutError.CodeResult();
                    }

                    if (error is ArgumentException)
                    {
                        return StatusCodes.ParamaterError.CodeResult();
                    }

                    return StatusCodes.SystemError.CodeResult();
            }
        }
    }
}

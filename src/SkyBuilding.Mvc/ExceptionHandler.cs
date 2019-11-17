#if NETSTANDARD2_0 || NETCOREAPP3_0
using Microsoft.Extensions.Logging;
#else
using log4net.Core;
#endif
using System;
using System.Collections.Generic;
using System.Net;

namespace SkyBuilding.Exceptions
{
    /// <summary> 异常处理类 </summary>
    public static class ExceptionHandler
    {
        private static readonly List<ILogger> loggers = new List<ILogger>();

        /// <summary> 异常事件 </summary>
        public static event Action<Exception> OnException;

        /// <summary> 服务异常事件 </summary>
        public static event Action<SkyException> OnSkyException;

        /// <summary> 服务异常事件 </summary>
        public static event Action<ServException> OnServException;

        /// <summary> 业务异常事件 </summary>
        public static event Action<BusiException> OnBusiException;

        /// <summary>
        /// 添加异常捕获日志（日志级别:Error）。
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public static void AddErrorLogger(ILogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

#if NETSTANDARD2_0 || NETCOREAPP3_0
            if (logger.IsEnabled(LogLevel.Error))
#else
            if (logger.IsEnabledFor(Level.Error))
#endif
            {
                loggers.Add(logger);
            }
        }

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

#if NETSTANDARD2_0 || NETCOREAPP3_0
                    loggers.ForEach(logger => logger.LogError(error, error.Message));
#else
                    loggers.ForEach(logger => logger.Log(typeof(ExceptionHandler), Level.Error, error.Message, error));
#endif

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

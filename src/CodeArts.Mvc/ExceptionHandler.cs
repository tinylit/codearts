using CodeArts.Mvc;
#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.Extensions.Logging;
#else
using log4net;
#endif
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Net;

namespace CodeArts.Exceptions
{
    /// <summary> 异常处理类 </summary>
    public static class ExceptionHandler
    {
#if NETSTANDARD2_0 || NETCOREAPP3_1
        private static ILogger logger;
#if NETCOREAPP3_1
        private static ILogger Logger => logger ??= LoggerManager.GetLogger(typeof(ExceptionHandler));
#else
        private static ILogger Logger => logger ?? (logger = LoggerManager.GetLogger(typeof(ExceptionHandler)));
#endif
#else
        private static ILog logger;
        private static ILog Logger => logger ?? (logger = LogManager.GetLogger(typeof(ExceptionHandler)));
#endif

        private static readonly List<ExceptionAdapter> Adapters = new List<ExceptionAdapter>();
        /// <summary>
        /// 添加异常适配器(系统约定的<see cref="CodeException"/>不接受适配器处理)。
        /// </summary>
        /// <param name="adapter">适配器</param>
        public static void Add(ExceptionAdapter adapter) => Adapters.Add(adapter ?? throw new ArgumentNullException(nameof(adapter)));

        /// <summary> 异常事件 </summary>
        public static event Action<Exception> OnException;

        /// <summary> 服务异常事件 </summary>
        public static event Action<CodeException> OnCodeException;

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
                case CodeException code:

                    switch (code)
                    {
                        case ServException serv:
                            OnServException?.Invoke(serv);
                            break;
                        case BusiException busi:
                            OnBusiException?.Invoke(busi);
                            break;
                        default:
                            OnCodeException?.Invoke(code);
                            break;
                    }

                    return DResult.Error(code.Message, code.ErrorCode);
                case OperationCanceledException _:
                    //操作取消
                    return null;
                default:

                    OnException?.Invoke(error);

                    foreach (var adapter in Adapters)
                    {
                        if (adapter.CanResolve(error))
                        {
                            return adapter.GetResult(error);
                        }
                    }

                    var type = error.GetType();

                    if (type.Name == "ValidationException" && type.FullName == "System.ComponentModel.DataAnnotations.ValidationException")
                    {
                        return DResult.Error(error.Message);
                    }

#if NETSTANDARD2_0 || NETCOREAPP3_1
                    Logger.LogError(error, error.Message);
#else
                    Logger.Error(error.Message, error);
#endif

                    if (error is WebException web)
                    {
                        if (web.Response is HttpWebResponse response)
                        {
                            return response.StatusCode.CodeResult();
                        }

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

                    if (error is NullReferenceException)
                    {
                        return StatusCodes.NullError.CodeResult();
                    }

                    if (error is DbException)
                    {
                        return StatusCodes.DbError.CodeResult();
                    }

                    if (error is SyntaxErrorException)
                    {
                        return StatusCodes.SyntaxError.CodeResult();
                    }

                    if (error is DivideByZeroException)
                    {
                        return StatusCodes.DivideByZeroError.CodeResult();
                    }

                    return StatusCodes.SystemError.CodeResult();
            }
        }
    }
}

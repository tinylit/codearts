using CodeArts.Serialize.Json;
using System;
#if NET40 || NET_NORMAL
namespace log4net.Core
#else
namespace Microsoft.Extensions.Logging
#endif
{
    /// <summary>
    /// 日志记录器扩展类。
    /// </summary>
    public static class LoggerExtentions
    {
#if NET40 || NET_NORMAL
        private static readonly Type ExtentionsType = typeof(LoggerExtentions);
#endif

        /// <summary>
        /// Debug。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        public static void Debug(this ILogger logger, Exception exception, object message) => logger.Debug(exception, JsonHelper.ToJson(message));

        /// <summary>
        /// Debug。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        public static void Debug(this ILogger logger, object message) => logger.Debug(JsonHelper.ToJson(message));

        /// <summary>
        /// Trace。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        public static void Trace(this ILogger logger, Exception exception, object message) => logger.Trace(exception, JsonHelper.ToJson(message));

        /// <summary>
        /// Trace。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        public static void Trace(this ILogger logger, object message) => logger.Trace(JsonHelper.ToJson(message));

        /// <summary>
        /// Info。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        public static void Info(this ILogger logger, Exception exception, object message) => logger.Info(exception, JsonHelper.ToJson(message));

        /// <summary>
        /// Info。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        public static void Info(this ILogger logger, object message) => logger.Info(JsonHelper.ToJson(message));

        /// <summary>
        /// Warn。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        public static void Warn(this ILogger logger, Exception exception, object message) => logger.Warn(exception, JsonHelper.ToJson(message));

        /// <summary>
        /// Warn。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        public static void Warn(this ILogger logger, object message) => logger.Warn(JsonHelper.ToJson(message));

        /// <summary>
        /// Error。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        public static void Error(this ILogger logger, Exception exception, object message) => logger.Error(exception, JsonHelper.ToJson(message));

        /// <summary>
        /// Error。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        public static void Error(this ILogger logger, object message) => logger.Error(JsonHelper.ToJson(message));

        /// <summary>
        /// Critical。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        public static void Critical(this ILogger logger, Exception exception, object message) => logger.Critical(exception, JsonHelper.ToJson(message));

        /// <summary>
        /// Critical。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        public static void Critical(this ILogger logger, object message) => logger.Critical(JsonHelper.ToJson(message));

        /// <summary>
        /// Debug。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Debug(this ILogger logger, Exception exception, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Debug, args?.Length > 0 ? string.Format(message, args) : message, exception);
#else
            logger.Log(LogLevel.Debug, exception, message, args);
#endif
        }

        /// <summary>
        /// Debug。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Debug(this ILogger logger, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Debug, args?.Length > 0 ? string.Format(message, args) : message, null);
#else
            logger.Log(LogLevel.Debug, message, args);
#endif
        }

        /// <summary>
        /// Trace。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Trace(this ILogger logger, Exception exception, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Trace, args?.Length > 0 ? string.Format(message, args) : message, exception);
#else
            logger.Log(LogLevel.Trace, exception, message, args);
#endif
        }

        /// <summary>
        /// Trace。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Trace(this ILogger logger, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Trace, args?.Length > 0 ? string.Format(message, args) : message, null);
#else
            logger.Log(LogLevel.Trace, message, args);
#endif
        }

        /// <summary>
        /// Info。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Info(this ILogger logger, Exception exception, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Info, args?.Length > 0 ? string.Format(message, args) : message, exception);
#else
            logger.Log(LogLevel.Information, exception, message, args);
#endif
        }

        /// <summary>
        /// Info。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Info(this ILogger logger, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Info, args?.Length > 0 ? string.Format(message, args) : message, null);
#else
            logger.Log(LogLevel.Information, message, args);
#endif
        }

        /// <summary>
        /// Warn。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Warn(this ILogger logger, Exception exception, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Warn, args?.Length > 0 ? string.Format(message, args) : message, exception);
#else
            logger.Log(LogLevel.Warning, exception, message, args);
#endif
        }

        /// <summary>
        /// Warn。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Warn(this ILogger logger, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Warn, args?.Length > 0 ? string.Format(message, args) : message, null);
#else
            logger.Log(LogLevel.Warning, message, args);
#endif
        }

        /// <summary>
        /// Error。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Error(this ILogger logger, Exception exception, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Error, args?.Length > 0 ? string.Format(message, args) : message, exception);
#else
            logger.Log(LogLevel.Error, exception, message, args);
#endif
        }

        /// <summary>
        /// Error。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Error(this ILogger logger, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Error, args?.Length > 0 ? string.Format(message, args) : message, null);
#else
            logger.Log(LogLevel.Error, message, args);
#endif
        }

        /// <summary>
        /// Critical。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Critical(this ILogger logger, Exception exception, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Critical, args?.Length > 0 ? string.Format(message, args) : message, exception);
#else
            logger.Log(LogLevel.Critical, exception, message, args);
#endif
        }

        /// <summary>
        /// Critical。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Critical(this ILogger logger, string message, params object[] args)
        {
#if NET40 || NET_NORMAL
            logger.Log(ExtentionsType, Level.Critical, args?.Length > 0 ? string.Format(message, args) : message, null);
#else
            logger.Log(LogLevel.Critical, message, args);
#endif
        }
    }
}

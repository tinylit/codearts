#if NET40 || NET_NORMAL

using System;

namespace log4net.Core
{
    /// <summary>
    /// 日志记录器扩展类。
    /// </summary>
    public static class LoggerExtentions
    {
        private static readonly Type ExtentionsType = typeof(LoggerExtentions);

        /// <summary>
        /// Debug。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="exception">异常。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Debug(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.Log(ExtentionsType, Level.Debug, args?.Length > 0 ? string.Format(message, args) : message, exception);
        }

        /// <summary>
        /// Debug。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Debug(this ILogger logger, string message, params object[] args)
        {
            logger.Log(ExtentionsType, Level.Debug, args?.Length > 0 ? string.Format(message, args) : message, null);
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
            logger.Log(ExtentionsType, Level.Trace, args?.Length > 0 ? string.Format(message, args) : message, exception);
        }

        /// <summary>
        /// Trace。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Trace(this ILogger logger, string message, params object[] args)
        {
            logger.Log(ExtentionsType, Level.Trace, args?.Length > 0 ? string.Format(message, args) : message, null);
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
            logger.Log(ExtentionsType, Level.Info, args?.Length > 0 ? string.Format(message, args) : message, exception);
        }

        /// <summary>
        /// Info。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Info(this ILogger logger, string message, params object[] args)
        {
            logger.Log(ExtentionsType, Level.Info, args?.Length > 0 ? string.Format(message, args) : message, null);
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
            logger.Log(ExtentionsType, Level.Warn, args?.Length > 0 ? string.Format(message, args) : message, exception);
        }

        /// <summary>
        /// Warn。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Warn(this ILogger logger, string message, params object[] args)
        {
            logger.Log(ExtentionsType, Level.Warn, args?.Length > 0 ? string.Format(message, args) : message, null);
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
            logger.Log(ExtentionsType, Level.Error, args?.Length > 0 ? string.Format(message, args) : message, exception);
        }

        /// <summary>
        /// Error。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Error(this ILogger logger, string message, params object[] args)
        {
            logger.Log(ExtentionsType, Level.Error, args?.Length > 0 ? string.Format(message, args) : message, null);
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
            logger.Log(ExtentionsType, Level.Critical, args?.Length > 0 ? string.Format(message, args) : message, exception);
        }

        /// <summary>
        /// Critical。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        /// <param name="message">记录消息。</param>
        /// <param name="args">参数。</param>
        public static void Critical(this ILogger logger, string message, params object[] args)
        {
            logger.Log(ExtentionsType, Level.Critical, args?.Length > 0 ? string.Format(message, args) : message, null);
        }
    }
}
#endif
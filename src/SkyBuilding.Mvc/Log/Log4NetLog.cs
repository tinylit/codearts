using log4net.Core;
using SkyBuilding.Log;
using SkyBuilding.Serialize.Json;
using System;

namespace SkyBuilding.Mvc.Log
{
    /// <summary>
    /// log4net日志系统
    /// </summary>
    public class Log4NetLog : BaseLog
    {
        //日志接口
        private readonly log4net.Core.ILogger _logger;

        public Log4NetLog(ILoggerWrapper wrapper)
        {
            _logger = wrapper.Logger;
        }


        /// <summary>
        /// 日志输出
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        protected override void Write<T>(LogLevel level, T message, Exception exception = null)
        {
            string content;
            if (message == null)
                content = "NULL";
            else
            {
                var type = typeof(T);
                if (type.IsValueType || type == typeof(string))
                    content = message.ToString();
                else
                    content = JsonHelper.ToJson(message);
            }

            _logger.Log(typeof(Log4NetLog), ParseLevel(level), content, exception);
        }


        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Trace"/>级别的日志
        /// </summary>
        public override bool IsTraceEnabled => _logger.IsEnabledFor(Level.Trace);

        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Debug"/>级别的日志
        /// </summary>
        public override bool IsDebugEnabled => _logger.IsEnabledFor(Level.Debug);

        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Info"/>级别的日志
        /// </summary>
        public override bool IsInfoEnabled => _logger.IsEnabledFor(Level.Info);

        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Warn"/>级别的日志
        /// </summary>
        public override bool IsWarnEnabled => _logger.IsEnabledFor(Level.Warn);

        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Error"/>级别的日志
        /// </summary>
        public override bool IsErrorEnabled => _logger.IsEnabledFor(Level.Error);

        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Fatal"/>级别的日志
        /// </summary>
        public override bool IsFatalEnabled => _logger.IsEnabledFor(Level.Fatal);

        /// <summary> 日志等级转换 </summary>
        /// <param name="level">等级</param>
        /// <returns></returns>
        public static Level ParseLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.All:
                    return Level.All;
                case LogLevel.Trace:
                    return Level.Trace;
                case LogLevel.Debug:
                    return Level.Debug;
                case LogLevel.Info:
                    return Level.Info;
                case LogLevel.Warn:
                    return Level.Warn;
                case LogLevel.Error:
                    return Level.Error;
                case LogLevel.Fatal:
                    return Level.Fatal;
                case LogLevel.Off:
                    return Level.Off;
                default:
                    return Level.Off;
            }
        }
    }
}

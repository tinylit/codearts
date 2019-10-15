using System;

namespace SkyBuilding.Log
{
    /// <summary>
    /// 日志记录器
    /// </summary>
    public sealed class Logger : ILogger
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; private set; }

        public Logger(Type type) : this(type.FullName) { }

        public Logger(string name) => Name = name;

        /// <summary>
        /// 写入<see cref="LogLevel.Trace"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Trace<T>(T message) => Name.EachAdapter(LogLevel.Trace, t => t.Trace(message));

        /// <summary>
        /// 写入<see cref="LogLevel.Debug"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Debug<T>(T message) => Name.EachAdapter(LogLevel.Debug, t => t.Debug(message));

        /// <summary>
        /// 写入<see cref="LogLevel.Info"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Info<T>(T message) => Name.EachAdapter(LogLevel.Info, t => t.Info(message));

        /// <summary>
        /// 写入<see cref="LogLevel.Warn"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Warn<T>(T message) => Name.EachAdapter(LogLevel.Warn, t => t.Warn(message));

        /// <summary>
        /// 写入<see cref="LogLevel.Error"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Error<T>(T message) => Name.EachAdapter(LogLevel.Error, t => t.Error(message));

        /// <summary>
        /// 写入<see cref="LogLevel.Error"/>日志消息，并记录异常
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常</param>
        public void Error<T>(T message, Exception exception) => Name.EachAdapter(LogLevel.Error, t => t.Error(message, exception));

        /// <summary>
        /// 写入<see cref="LogLevel.Fatal"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public void Fatal<T>(T message) => Name.EachAdapter(LogLevel.Fatal, t => t.Fatal(message));

        /// <summary>
        /// 写入<see cref="LogLevel.Fatal"/>日志消息，并记录异常
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常</param>
        public void Fatal<T>(T message, Exception exception) => Name.EachAdapter(LogLevel.Fatal, t => t.Fatal(message, exception));
    }
}

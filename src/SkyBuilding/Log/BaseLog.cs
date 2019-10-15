using System;

namespace SkyBuilding.Log
{
    /// <summary>
    /// 日志基类（后续日志实现需继承该基类）
    /// </summary>
    public abstract class BaseLog : ILog
    {
        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="level">日志输出级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="exception">日志异常</param>
        protected abstract void Write<T>(LogLevel level, T message, Exception exception = null);

        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Trace"/>级别的日志
        /// </summary>
        public abstract bool IsTraceEnabled { get; }

        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Debug"/>级别的日志
        /// </summary>
        public abstract bool IsDebugEnabled { get; }

        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Info"/>级别的日志
        /// </summary>
        public abstract bool IsInfoEnabled { get; }

        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Warn"/>级别的日志
        /// </summary>
        public abstract bool IsWarnEnabled { get; }

        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Error"/>级别的日志
        /// </summary>
        public abstract bool IsErrorEnabled { get; }

        /// <summary>
        /// 获取 是否允许输出<see cref="LogLevel.Fatal"/>级别的日志
        /// </summary>
        public abstract bool IsFatalEnabled { get; }

        /// <summary>
        /// 写入<see cref="LogLevel.Trace"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public virtual void Trace<T>(T message)
        {
            if (IsTraceEnabled)
                Write(LogLevel.Trace, message);
        }

        /// <summary>
        /// 写入<see cref="LogLevel.Debug"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public virtual void Debug<T>(T message)
        {
            if (IsDebugEnabled)
                Write(LogLevel.Debug, message);
        }

        /// <summary>
        /// 写入<see cref="LogLevel.Info"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public virtual void Info<T>(T message)
        {
            if (IsInfoEnabled)
                Write(LogLevel.Info, message);
        }

        /// <summary>
        /// 写入<see cref="LogLevel.Warn"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public virtual void Warn<T>(T message)
        {
            if (IsWarnEnabled)
                Write(LogLevel.Warn, message);
        }

        /// <summary>
        /// 写入<see cref="LogLevel.Error"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public virtual void Error<T>(T message)
        {
            if (IsErrorEnabled)
                Write(LogLevel.Error, message);
        }

        /// <summary>
        /// 写入<see cref="LogLevel.Error"/>日志消息，并记录异常
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常</param>
        public virtual void Error<T>(T message, Exception exception)
        {
            if (IsErrorEnabled)
                Write(LogLevel.Error, message, exception);
        }

        /// <summary>
        /// 写入<see cref="LogLevel.Fatal"/>日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        public virtual void Fatal<T>(T message)
        {
            if (IsFatalEnabled)
                Write(LogLevel.Fatal, message);
        }

        /// <summary>
        /// 写入<see cref="LogLevel.Fatal"/>日志消息，并记录异常
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常</param>
        public virtual void Fatal<T>(T message, Exception exception)
        {
            if (IsFatalEnabled)
                Write(LogLevel.Fatal, message, exception);
        }
    }
}

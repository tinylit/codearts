using System;

namespace SkyBuilding.Log
{
    /// <summary>
    /// 日志接口(定义日志基本功能)
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 追踪泛型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        void Trace<T>(T message);

        /// <summary>
        /// 调试日志
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        void Debug<T>(T message);

        /// <summary>
        /// 信息日志
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        void Info<T>(T message);

        /// <summary>
        /// 警告日志
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        void Warn<T>(T message);

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        void Error<T>(T message);

        /// <summary>
        /// 错误日志（带异常）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="exception">异常</param>
        void Error<T>(T message, Exception exception);

        /// <summary>
        /// 致命日志
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        void Fatal<T>(T message);

        /// <summary>
        /// 致命日志（带异常）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        void Fatal<T>(T message, Exception exception);
    }
}

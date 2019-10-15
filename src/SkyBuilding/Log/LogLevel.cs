using System;

namespace SkyBuilding.Log
{
    /// <summary> 表示日志输出级别的枚举 </summary>
    [Flags]
    public enum LogLevel
    {
        /// <summary>
        /// 关闭所有日志，不输出日志
        /// </summary>
        Off = 0,

        /// <summary>
        /// 表示跟踪的日志级别
        /// </summary>
        Trace = 1 << 0,

        /// <summary>
        /// 表示调试的日志级别
        /// </summary>
        Debug = 1 << 1,

        /// <summary>
        /// 表示消息的日志级别
        /// </summary>
        Info = 1 << 2,

        /// <summary>
        /// 表示警告的日志级别
        /// </summary>
        Warn = 1 << 3,

        /// <summary>
        /// 表示错误的日志级别
        /// </summary>
        Error = 1 << 4,

        /// <summary>
        /// 表示严重错误的日志级别
        /// </summary>
        Fatal = 1 << 5,

        /// <summary>
        /// 输出所有级别的日志
        /// </summary>
        All = 1 << 6
    }
}

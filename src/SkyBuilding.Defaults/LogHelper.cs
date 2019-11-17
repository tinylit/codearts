#if NET40 || NET45 || NET451 || NET452 || NET461
using log4net.Appender;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using System;

namespace log4net
{
    /// <summary>
    /// 日志助手
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// 基本输出格式
        /// </summary>
        private static readonly ILayout NormalLayout =
            new PatternLayout("%date [%-5level] [%property{LogSite}] %r %thread %logger %message %exception%n");

        /// <summary>
        /// 错误输出格式
        /// </summary>
        private static readonly ILayout ErrorLayout =
            new PatternLayout("%date [%-5level] [%property{LogSite}] %r %thread %logger %message %n%exception%n");

        /// <summary>
        /// 基础信息输出
        /// </summary>
        /// <param name="name"></param>
        /// <param name="file"></param>
        /// <param name="layout"></param>
        /// <returns></returns>
        private static RollingFileAppender BaseAppender(string name, string file, ILayout layout)
        {
            return new RollingFileAppender
            {
                Name = name,
                File = $"_logs/{DateTime.Now.ToString("yyyyMM")}/",
                AppendToFile = true,
                LockingModel = new FileAppender.MinimalLock(),
                RollingStyle = RollingFileAppender.RollingMode.Date,
                DatePattern = file,
                StaticLogFileName = false,
                MaxSizeRollBackups = 100,
                MaximumFileSize = "2MB",
                Layout = layout
            };
        }

        /// <summary>
        /// 调试信息输出。
        /// level: Level.Finest - Level.Trace
        /// </summary>
        /// <returns></returns>
        public static IAppender TraceAppender()
        {
            const string file = "dd\"_trace.log\"";
            var appender = BaseAppender("traceRollingFile", file, NormalLayout);
            appender.ClearFilters();

            appender.AddFilter(new LevelRangeFilter
            {
                LevelMin = Level.Finest,
                LevelMax = Level.Trace
            });
            appender.ActivateOptions();
            return appender;
        }

        /// <summary>
        /// 调试信息输出。
        /// level: Level.Debug - Level.Warn
        /// </summary>
        /// <returns></returns>
        public static IAppender DebugAppender()
        {
            const string file = "dd\".log\"";
            var appender = BaseAppender("rollingFile", file, NormalLayout);
            appender.ClearFilters();

            appender.AddFilter(new LevelRangeFilter
            {
                LevelMin = Level.Debug,
                LevelMax = Level.Warn
            });
            appender.ActivateOptions();
            return appender;
        }

        /// <summary>
        /// 错误信息输出。
        /// level: Level.Error - Level.Fatal
        /// </summary>
        /// <returns></returns>
        public static IAppender ErrorAppender()
        {
            const string file = "dd\"_error.log\"";
            var appender = BaseAppender("errorRollingFile", file, ErrorLayout);
            appender.AddFilter(new LevelRangeFilter
            {
                LevelMin = Level.Error,
                LevelMax = Level.Fatal
            });
            appender.ActivateOptions();
            return appender;
        }

        /// <summary>
        /// 将字符串尝试转为对应的日志级别。
        /// </summary>
        /// <param name="text">文本</param>
        /// <param name="level">级别</param>
        /// <returns></returns>
        public static bool TryParse(string text, out Level level)
        {
            level = Level.Off;
            if (text is null)
            {
                return false;
            }

            var isParsed = true;

            switch (text.ToLower())
            {
                case "off":
                    level = Level.Off;
                    break;
                case "all":
                    level = Level.All;
                    break;
                case "verbose":
                    level = Level.Verbose;
                    break;
                case "finer":
                    level = Level.Finer;
                    break;
                case "trace":
                    level = Level.Trace;
                    break;
                case "fine":
                    level = Level.Fine;
                    break;
                case "debug":
                    level = Level.Debug;
                    break;
                case "info":
                    level = Level.Info;
                    break;
                case "notice":
                    level = Level.Notice;
                    break;
                case "finest":
                    level = Level.Finest;
                    break;
                case "error":
                    level = Level.Error;
                    break;
                case "severe":
                    level = Level.Severe;
                    break;
                case "critical":
                    level = Level.Critical;
                    break;
                case "alert":
                    level = Level.Alert;
                    break;
                case "fatal":
                    level = Level.Fatal;
                    break;
                case "emergency":
                    level = Level.Emergency;
                    break;
                default:
                    isParsed = false;
                    break;
            }

            return isParsed;
        }

        /// <summary>
        /// 获取配置文件中“log-level”配置级别，默认："ALL"。
        /// </summary>
        /// <returns></returns>
        public static IAppender DefaultAppender()
        {
            if (TryParse("log-level".Config("ALL"), out Level level))
            {
                return LevelAppender(level);
            }

            return LevelAppender(Level.All);
        }

        /// <summary>
        /// 获取指定级别的日志输出。
        /// </summary>
        /// <param name="level">级别</param>
        /// <returns></returns>
        public static IAppender LevelAppender(Level level) => LevelAppender(level, level);

        /// <summary>
        /// 获取指定级别的日志输出。
        /// </summary>
        /// <param name="levelMin">最小级别</param>
        /// <param name="levelMax">最大级别</param>
        /// <returns></returns>
        public static IAppender LevelAppender(Level levelMin, Level levelMax)
        {
            string file = levelMin == levelMax ? $"dd\"_{levelMin.Name}.log\"" : $"dd\"{levelMin.Name}_{levelMax.Name}.log\"";
            var appender = BaseAppender("traceRollingFile", file, NormalLayout);
            appender.ClearFilters();

            appender.AddFilter(new LevelRangeFilter
            {
                LevelMin = levelMin,
                LevelMax = levelMax
            });
            appender.ActivateOptions();
            return appender;
        }
    }
}
#endif
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Filter;
using log4net.Layout;
using SkyBuilding.Log;
using System;

namespace SkyBuilding.Mvc.Log
{
    /// <summary>
    /// log4net日志适配器
    /// </summary>
    public class Log4NetAdapter : LoggerAdapter
    {
        /// <summary>
        /// log4net配置
        /// </summary>
        private static class Log4NetDefaultConfig
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
            /// 调试信息输出
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
            /// 调试信息输出
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
            /// 错误信息输出
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
        }

        private static string LogSite => "site".Config("local");

        /// <summary>
        /// 初始化一个类型的新实例
        /// </summary>
        public Log4NetAdapter()
        {
            log4net.GlobalContext.Properties["LogSite"] = LogSite;

            BasicConfigurator.Configure(log4net.LogManager.CreateRepository(LogSite), Log4NetDefaultConfig.TraceAppender(), Log4NetDefaultConfig.DebugAppender(), Log4NetDefaultConfig.ErrorAppender());
        }

        public override ILog GetLogger(string name) => new Log4NetLog(log4net.LogManager.GetLogger(LogSite, name));
    }
}

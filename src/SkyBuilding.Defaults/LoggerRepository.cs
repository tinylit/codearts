#if NET40 || NET45 || NET451 || NET452 || NET461
using System;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using SkyBuilding.DesignMode;

namespace log4net
{
    /// <summary>
    /// 日志记录仓库（默认配置的日志记录仓库）。
    /// </summary>
    public class LoggerRepository : Singleton<LoggerRepository>
    {
        private readonly ILoggerRepository repository;
        /// <summary>
        /// 构造函数
        /// </summary>
        private LoggerRepository()
        {
            var logSite = "log-site".Config("local");

            GlobalContext.Properties["LogSite"] = logSite;

            repository = LogManager.CreateRepository(logSite);

            LogHelper.TryParse("log-level".Config("ALL"), out Level level);

            if (level == Level.All)
            {
                BasicConfigurator.Configure(repository, LogHelper.TraceAppender(), LogHelper.DebugAppender(), LogHelper.ErrorAppender());
            }
            else if (level > Level.Off)
            {
                BasicConfigurator.Configure(repository, LogHelper.LevelAppender(level));
            }
        }

        /// <summary>
        /// 获取日志记录器。
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public ILogger GetLogger(string name) => repository.GetLogger(name);
    }
}
#endif

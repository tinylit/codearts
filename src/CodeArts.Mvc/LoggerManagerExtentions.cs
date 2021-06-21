#if NETCOREAPP2_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// 日志管理器。
    /// 使用<see cref="UseLoggerManager(IApplicationBuilder)"/>或<see cref="UseLoggerManager(IServiceCollection)"/>初始化后，可用；否则返回<see cref="NullLogger"/>或<see cref="NullLogger{T}"/>日志实例。
    /// </summary>
    public static class LoggerManagerExtentions
    {
        /// <summary>
        /// 使用日志管理器<see cref="LoggerManager.RegisterLogging(Func{ILoggerFactory})"/>。
        /// </summary>
        /// <param name="app">程序构造器。</param>
        /// <returns></returns>
        public static void UseLoggerManager(this IApplicationBuilder app) => LoggerManager.RegisterLogging(() => app.ApplicationServices.GetRequiredService<ILoggerFactory>());

        /// <summary>
        /// 使用日志管理器<see cref="LoggerManager.RegisterLogging(Func{ILoggerFactory})"/>。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns></returns>
        public static void UseLoggerManager(this IServiceCollection services) => LoggerManager.RegisterLogging(() => services.BuildServiceProvider().GetRequiredService<ILoggerFactory>());

#if NETCOREAPP3_1
        /// <summary>
        /// 使用日志管理器<see cref="LoggerManager.RegisterLogging(Func{ILoggerFactory})"/>。
        /// </summary>
        /// <param name="builder">日志构造器。</param>
        /// <returns></returns>
        public static void UseLoggerManager(this ILoggingBuilder builder) => LoggerManager.RegisterLogging(() => builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>());
#endif
    }
}
#endif
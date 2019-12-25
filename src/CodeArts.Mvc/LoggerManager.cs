#if NETSTANDARD2_0 || NETCOREAPP3_1
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
    public static class LoggerManager
    {
        private static IServiceProvider _serviceProvider;

        /// <summary>
        /// 使用日志管理器。
        /// </summary>
        /// <param name="app">程序构造器</param>
        /// <returns></returns>
        public static IApplicationBuilder UseLoggerManager(this IApplicationBuilder app)
        {
            _serviceProvider = app.ApplicationServices;

            return app;
        }

        /// <summary>
        /// 使用日志管理器。
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns></returns>
        public static IServiceCollection UseLoggerManager(this IServiceCollection services)
        {
            _serviceProvider = services.BuildServiceProvider();

            return services;
        }

#if NETCOREAPP3_1
        /// <summary>
        /// 使用日志管理器。
        /// </summary>
        /// <param name="builder">日志构造器</param>
        /// <returns></returns>
        public static ILoggingBuilder UseLoggerManager(this ILoggingBuilder builder)
        {
            _serviceProvider = builder.Services.BuildServiceProvider();

            return builder;
        }
#endif

        private static ILoggerFactory factory;
        private static ILoggerFactory Factory => factory ?? (factory = _serviceProvider?.GetRequiredService<ILoggerFactory>());

        /// <summary>
        /// 获取一个日志记录器。
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns></returns>
        public static ILogger<T> GetLogger<T>() => Factory?.CreateLogger<T>() ?? NullLogger<T>.Instance;

        /// <summary>
        /// 获取一个日志记录器。
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static ILogger GetLogger(Type type) => Factory?.CreateLogger(type) ?? NullLogger.Instance;
    }
}
#endif
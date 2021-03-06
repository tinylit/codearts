﻿#if NETSTANDARD2_0
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;

namespace CodeArts.SignalR
{
    /// <summary>
    /// SignalRAppBuilder 扩展。
    /// </summary>
    public static class SignalRAppBuilderExtensions
    {
        /// <summary>
        /// 注册所有hub链接。 <see cref="HubRouteBuilder.MapHub{THub}(PathString)"/> Hub类名小写作为参数（如果类名以hub结束，将会去除hub后作为参数）。
        /// </summary>
        /// <param name="app">程序配置。</param>
        /// <returns></returns>
        public static IApplicationBuilder UseSignalR(this IApplicationBuilder app)
            => app.UseSignalR(builder =>
            {
                var info = builder
                .GetType()
                .GetMethod("MapHub", new Type[] { typeof(PathString) });

                string path = AppDomain.CurrentDomain.BaseDirectory;

                foreach (var assembly in AssemblyFinder.Find("pattern".Config("*")))
                {
                    foreach (var type in assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(Hub).IsAssignableFrom(type)))
                    {
                        var methodInfo = info.MakeGenericMethod(type);

                        string name = type.Name.ToLower();

                        if (name.EndsWith("hub"))
                        {
                            name = name.Substring(0, name.Length - 3);
                        }

                        methodInfo.Invoke(builder, new object[] { new PathString($"/{name}") });
                    }
                }
            });

        /// <summary>
        /// 注册所有hub链接。 <see cref="HubRouteBuilder.MapHub{THub}(PathString, Action{HttpConnectionDispatcherOptions})"/> Hub类名小写作为参数（如果类名以hub结束，将会去除hub后作为参数）。
        /// </summary>
        /// <param name="app">程序配置。</param>
        /// <param name="configureOptions">配置。</param>
        /// <returns></returns>
        public static IApplicationBuilder UseSignalR(this IApplicationBuilder app, Action<HttpConnectionDispatcherOptions> configureOptions)
            => app.UseSignalR(builder =>
            {
                var info = builder
                .GetType()
                .GetMethod("MapHub", new Type[] { typeof(PathString), typeof(Action<HttpConnectionDispatcherOptions>) });

                string path = AppDomain.CurrentDomain.BaseDirectory;

                foreach (var assembly in AssemblyFinder.Find("pattern".Config("*")))
                {
                    foreach (var type in assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(Hub).IsAssignableFrom(type)))
                    {
                        var methodInfo = info.MakeGenericMethod(type);

                        string name = type.Name.ToLower();

                        if (name.EndsWith("hub"))
                        {
                            name = name.Substring(0, name.Length - 3);
                        }

                        methodInfo.Invoke(builder, new object[] { new PathString($"/{name}"), configureOptions });
                    }
                }
            });
    }
}
#endif
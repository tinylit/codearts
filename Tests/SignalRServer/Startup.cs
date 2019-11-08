using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;
using SkyBuilding.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace SignalRServer
{
    /// <summary>
    /// 启动类
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// 配置
        /// </summary>
        /// <param name="app"></param>
        public void Configuration(IAppBuilder app)
        {
            app.Map("/signalr", map =>
            {
                // Turns cors support on allowing everything
                // In real applications, the origins should be locked down
                map.UseCors(CorsOptions.AllowAll)
                   .UseJwtSignalR(); //? JWT消息中间件
            });

            // Turn tracing on programmatically
            GlobalHost.TraceManager.Switch.Level = SourceLevels.Information;

            app.MapSignalR();
        }
    }
}
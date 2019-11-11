using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Owin;
using System.Diagnostics;

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
                   .UseJwtBearer(new JwtBearerEvents
                   {
                       OnMessageReceived = context => context.Token = context.Request.Query.Get("token"),
                       OnTokenValidate = context =>
                       {
                           var data = context.UserData;

                           if (!data.ContainsKey("name") && data.TryGetValue("jti", out object value))
                           {
                               //? 修改 Name 显示内容
                               data.Add("name", value);
                           }
                       }
                   }) //? JWT消息中间件
                   .RunSignalR(); //? 启动通讯
            });

            // Turn tracing on programmatically
            GlobalHost.TraceManager.Switch.Level = SourceLevels.Information;

            app.MapSignalR();
        }
    }
}
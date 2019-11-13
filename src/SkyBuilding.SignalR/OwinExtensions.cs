#if NET45 || NET451 || NET452 ||NET461
using SkyBuilding.SignalR;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Owin
{
    /// <summary>
    /// Owin 扩展
    /// </summary>
    public static class JwtExtensions
    {
        /// <summary>
        /// 用户数据转为认证信息实体。
        /// </summary>
        /// <param name="userData">用户数据</param>
        /// <returns></returns>
        public static ClaimsIdentity AsIdentity(IDictionary<string, object> userData) => userData.AsIdentity();

        /// <summary>
        /// 使用 JWT 认证。
        /// </summary>
        /// <param name="app">项目构建器</param>
        /// <param name="events">认证事件</param>
        /// <returns></returns>
        public static IAppBuilder UseJwtBearer(this IAppBuilder app, JwtBearerEvents events)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            return app.Use((context, next) =>
            {
                var mcontext = new MessageReceivedContext(context);

                events.MessageReceived(mcontext);

                if (string.IsNullOrEmpty(mcontext.Token))
                {
                    return next();
                }

                var vcontext = new TokenValidateContext(mcontext);

                events.TokenValidate(vcontext);

                if (vcontext.UserData is null || vcontext.UserData.Count == 0)
                {
                    return next();
                }

                var vdcontext = new TokenValidatedContext(vcontext);

                events.TokenValidated(vdcontext);

                if (vdcontext.User?.Identity?.IsAuthenticated ?? false)
                {
                    context.Authentication.User = vdcontext.User;
                }

                return next();
            });
        }
    }
}
#endif
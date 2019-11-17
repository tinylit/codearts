#if NET45 || NET451 || NET452 || NET461
using SkyBuilding.Mvc.Authentication;
using System;

namespace SkyBuilding.Mvc.Builder
{
    /// <summary>
    /// Jwt 认证扩展
    /// </summary>
    public static class JwtBearerApplicationBuilderExtensions
    {
        /// <summary>
        /// 使用 JWT 认证。
        /// </summary>
        /// <param name="app">项目构建器</param>
        /// <param name="events">认证配置</param>
        /// <returns></returns>
        public static IApplicationBuilder UseJwtBearer(this IApplicationBuilder app, JwtBearerEvents events)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            return app.Use(next =>
            {
                return context =>
                {
                    var receivedContext = new MessageReceivedContext(context);

                    events.MessageReceived(receivedContext);

                    if (string.IsNullOrEmpty(receivedContext.Token))
                    {
                        return next.Invoke(context);
                    }

                    var tokenValidateContext = new TokenValidateContext(receivedContext);

                    events.TokenValidate(tokenValidateContext);

                    if (tokenValidateContext.UserData is null || tokenValidateContext.UserData.Count == 0)
                    {
                        return next.Invoke(context);
                    }

                    var tokenValidatedContext = new TokenValidatedContext(tokenValidateContext);

                    events.TokenValidated(tokenValidatedContext);

                    if (tokenValidatedContext.User?.Identity?.IsAuthenticated ?? false)
                    {
                        context.User = tokenValidatedContext.User;
                    }

                    return next.Invoke(context);
                };
            });
        }
    }
}
#endif
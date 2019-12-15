#if NET40 || NET45 || NET451 || NET452 || NET461
using CodeArts.Mvc.Authentication;
using System;
#if NET40
using System.Threading;
#endif

namespace CodeArts.Mvc.Builder
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
#if NET40
                        next.Invoke(context);
                        return;
#else
                        return next.Invoke(context);
#endif
                    }

                    var tokenValidateContext = new TokenValidateContext(receivedContext);

                    events.TokenValidate(tokenValidateContext);

                    if (tokenValidateContext.UserData is null || tokenValidateContext.UserData.Count == 0)
                    {
#if NET40
                        next.Invoke(context);
                        return;
#else
                        return next.Invoke(context);
#endif
                    }

                    var tokenValidatedContext = new TokenValidatedContext(tokenValidateContext);

                    events.TokenValidated(tokenValidatedContext);

                    if (tokenValidatedContext.User?.Identity?.IsAuthenticated ?? false)
                    {
                        context.User = tokenValidatedContext.User;
#if NET40
                        Thread.CurrentPrincipal = tokenValidatedContext.User;
#endif
                    }

#if NET40
                    next.Invoke(context);
                    return;
#else
                    return next.Invoke(context);
#endif
                };
            });
        }
    }
}
#endif
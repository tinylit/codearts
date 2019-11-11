#if NET45 || NET451 || NET452 ||NET461
using JWT;
using JWT.Serializers;
using SkyBuilding;
using SkyBuilding.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;

namespace Owin
{
    /// <summary>
    /// Owin 扩展
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Owin", Justification = "The owin namespace is for consistentcy.")]
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
        /// <returns></returns>
        public static IAppBuilder UseJwtBearer(this IAppBuilder app) => app.UseJwtBearer(AsIdentity);

        /// <summary>
        /// 使用 JWT 认证。
        /// </summary>
        /// <param name="app">项目构建器</param>
        /// <param name="asIdentity">将用户数据转为认证实体</param>
        /// <returns></returns>
        public static IAppBuilder UseJwtBearer(this IAppBuilder app, Func<IDictionary<string, object>, ClaimsIdentity> asIdentity)
        {
            return app.Use((context, next) =>
            {
                //前端请求api时会将token存放在名为"auth"的请求头中
                var token = context.Request.Query.Get("token");

                if (token is null || token.Length == 0)
                {
                    return next();
                }

                var serializer = new JsonNetSerializer();
                var provider = new UtcDateTimeProvider();
                var validator = new JwtValidator(serializer, provider);
                var urlEncoder = new JwtBase64UrlEncoder();
                var decoder = new JwtDecoder(serializer, validator, urlEncoder);

                try
                {
                    var user = decoder.DecodeToObject(token, "jwt-secret".Config(Consts.JwtSecret), false);

                    var identity = asIdentity.Invoke(user);

                    if (!identity.Claims.Any(x => x.Type == ClaimTypes.Name))
                    {
                        if (user.Any(x => x.Key.ToLower() == "id"))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Name, user.First(x => x.Key.ToLower() == "id").Value.ToString()));
                        }
                        else if (user.Any(x => x.Key.ToLower() == "name"))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Name, user.First(x => x.Key.ToLower() == "name").Value.ToString()));
                        }
                        else if (user.Any(x => x.Key.ToLower() == "jti"))
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Name, user.First(x => x.Key.ToLower() == "jti").Value.ToString()));
                        }
                    }

                    context.Authentication.User = new ClaimsPrincipal(identity);
                }
                catch { }

                return next();
            });
        }
    }
}
#endif
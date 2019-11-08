#if NET45 || NET451 || NET452 ||NET461
using JWT;
using JWT.Serializers;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Owin.Middleware;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SkyBuilding.SignalR.Owin.Middleware
{
    /// <summary>
    /// JWT 认证 <see cref="PersistentConnectionMiddleware"/>
    /// </summary>
    public class JwtPersistentConnectionMiddleware : PersistentConnectionMiddleware
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="next">中间件</param>
        /// <param name="connectionType">链接类型</param>
        /// <param name="configuration">链接</param>
        public JwtPersistentConnectionMiddleware(OwinMiddleware next, Type connectionType, ConnectionConfiguration configuration) : base(next, connectionType, configuration)
        {
        }

        /// <summary>
        /// 调用
        /// </summary>
        /// <param name="context">上下文</param>
        /// <returns></returns>
        public override Task Invoke(IOwinContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            //前端请求api时会将token存放在名为"auth"的请求头中
            var token = context.Request.Query.Get("token");

            if (token is null || token.Length == 0)
            {
                return base.Invoke(context);
            }

            var serializer = new JsonNetSerializer();
            var provider = new UtcDateTimeProvider();
            var validator = new JwtValidator(serializer, provider);
            var urlEncoder = new JwtBase64UrlEncoder();
            var decoder = new JwtDecoder(serializer, validator, urlEncoder);

            try
            {
                var user = decoder.DecodeToObject(token, "jwt-secret".Config(Consts.JwtSecret), false);

                context.Environment.Add("server.User", new ClaimsPrincipal(user.AsIdentity()));
            }
            catch { }

            return base.Invoke(context);
        }
    }
}
#endif
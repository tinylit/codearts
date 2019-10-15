
#if NET40 || NET45 || NET451 || NET452 || NET461
using JWT;
using JWT.Serializers;
using SkyBuilding;
using SkyBuilding.Serialize.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace SkyBuilding.Mvc
{
    /// <summary>
    /// JWT认证
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class JwtAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext context)
        {
            //前端请求api时会将token存放在名为"auth"的请求头中
            var token = context.Request.Headers.Authorization;

            if (token is null)
            {
                context.Response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent(JsonHelper.ToJson(HttpStatusCode.Unauthorized.CodeResult()))
                };
                return false;
            }

            if (string.IsNullOrEmpty(token.Scheme))
            {
                context.Response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent(JsonHelper.ToJson(HttpStatusCode.Unauthorized.CodeResult()))
                };

                return false;
            }

            var serializer = new JsonNetSerializer();
            var provider = new UtcDateTimeProvider();
            var validator = new JwtValidator(serializer, provider);
            var urlEncoder = new JwtBase64UrlEncoder();
            var decoder = new JwtDecoder(serializer, validator, urlEncoder);

            try
            {
                var userData = decoder.DecodeToObject(token.Scheme, "jwt-secret".Config(Consts.Secret), true);

                var identity = userData.AsIdentity();
#if NET40
                Thread.CurrentPrincipal = new ClaimsPrincipal(identity);
#else
                context.ControllerContext.RequestContext.Principal = new ClaimsPrincipal(identity);
#endif
            }
            catch
            {
                context.Response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Content = new StringContent(JsonHelper.ToJson(HttpStatusCode.Unauthorized.CodeResult()))
                };

                return false;
            }

            return base.IsAuthorized(context);
        }
    }
}

#endif
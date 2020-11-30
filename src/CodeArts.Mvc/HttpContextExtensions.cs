#if NET_CORE
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
#else
using System;
using System.Web;
#endif

namespace CodeArts.Mvc
{
    /// <summary>
    /// 请求上下文扩展。
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// 获取客户端IP地址。
        /// </summary>
        /// <param name="context">请求上下文。</param>
        /// <returns></returns>
#if NET_CORE
        public static string GetRemoteIpAddress(this HttpContext context)
        {
            string ipAddress = context.Connection.RemoteIpAddress.ToString();

            if (ipAddress == "::1")
            {
                return "127.0.0.1";
            }

            return ipAddress;
        }
#else
        public static string GetRemoteIpAddress(this HttpContext context)
        {
            string ipAddress = context.Request.ServerVariables["REMOTE_ADDR"];

            if (ipAddress.IsEmpty())
            {
                if (context.Request.ServerVariables["HTTP_VIA"] != null)
                {
                    ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"]?.ToString();

                    if (ipAddress.IsNotEmpty())
                    {
                        ipAddress = ipAddress.Split(',')[0].Trim();
                    }
                }
            }

            if (ipAddress.IsEmpty())
            {
                ipAddress = context.Request.UserHostAddress;
            }

            if (ipAddress == "::1")
            {
                return "127.0.0.1";
            }

            return ipAddress;
        }
#endif

        /// <summary>
        /// 获取请求方的地址。
        /// </summary>
        /// <returns></returns>
#if NET_CORE
        public static string GetRefererUrlStrings(this HttpContext context)
        {
            StringValues origin = context.Request.Headers[HeaderNames.Referer];
            if (origin == StringValues.Empty)
            {
                return string.Empty;
            }
            return origin.ToString();
        }
    }
#else
        public static string GetRefererUrlStrings(this HttpContext context) => context.Request.UrlReferrer?.ToString();
    }
#endif
}
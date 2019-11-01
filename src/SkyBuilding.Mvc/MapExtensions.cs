#if NETSTANDARD2_0 || NETCOREAPP3_0
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using SkyBuilding.Exceptions;
using System;
using System.IO;
using System.Net;

namespace SkyBuilding.Mvc
{
    /// <summary>
    /// 拓展
    /// </summary>
    public static class MapExtensions
    {
        /// <summary>
        /// 路由(HttpVerbs.Get | HttpVerbs.Post | HttpVerbs.Put | HttpVerbs.Delete | HttpVerbs.Head | HttpVerbs.Patch | HttpVerbs.Options)
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="destinationPath">目标地址</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, string destinationPath)
        {
            if (destinationPath is null)
            {
                throw new ArgumentNullException(nameof(destinationPath));
            }

            return app.Map(path, _ => destinationPath);
        }

        /// <summary>
        /// 路由(HttpVerbs.Get | HttpVerbs.Post | HttpVerbs.Put | HttpVerbs.Delete | HttpVerbs.Head | HttpVerbs.Patch | HttpVerbs.Options)
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="destinationPath">目标地址</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, Func<PathString, string> destinationPath)
        {
            if (destinationPath is null)
            {
                throw new ArgumentNullException(nameof(destinationPath));
            }

            return app.Map(path, HttpVerbs.Get | HttpVerbs.Post | HttpVerbs.Put | HttpVerbs.Delete | HttpVerbs.Head | HttpVerbs.Patch | HttpVerbs.Options, destinationPath);
        }

        /// <summary>
        /// 路由
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="httpVerbs">请求方式</param>
        /// <param name="destinationPath">目标地址</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, HttpVerbs httpVerbs, string destinationPath)
        {
            if (destinationPath is null)
            {
                throw new ArgumentNullException(nameof(destinationPath));
            }

            return app.Map(path, httpVerbs, _ => destinationPath);
        }

        /// <summary>
        /// 路由
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="httpVerbs">请求方式</param>
        /// <param name="destinationPath">目标地址</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, HttpVerbs httpVerbs, Func<PathString, string> destinationPath)
        {
            if (destinationPath is null)
            {
                throw new ArgumentNullException(nameof(destinationPath));
            }

            return app.Map(path, builder => builder.Run(async context =>
            {
                if (!Enum.TryParse(context.Request.Method ?? "GET", true, out HttpVerbs verbs) || (httpVerbs & verbs) == 0)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                string destinationUrl = destinationPath.Invoke(path);

                if (string.IsNullOrEmpty(destinationUrl))
                {
                    context.Response.StatusCode = 404;

                    return;
                }

                if (destinationUrl.IsUrl() ? !Uri.TryCreate(destinationUrl, UriKind.Absolute, out Uri uri) : !Uri.TryCreate($"{context.Request.Scheme}://{context.Request.Host}/{destinationUrl.TrimStart('/')}", UriKind.Absolute, out uri))
                {
                    await context.Response.WriteJsonAsync(DResult.Error($"不规范的接口({path.Value}=>{destinationUrl})!", StatusCodes.NonstandardServerError));
                    return;
                }

                var request = uri.AsRequestable().Query(context.Request.QueryString.Value);

                var token = context.Request.Headers[HeaderNames.Authorization];

                if (!string.IsNullOrEmpty(token))
                {
                    request.Header(HeaderNames.Authorization, token);
                }

                if (verbs != HttpVerbs.Get && verbs != HttpVerbs.Delete)
                {
                    string contentType = context.Request.ContentType?.ToLower() ?? "application/json";

                    using (var reader = new StreamReader(context.Request.Body))
                    {
                        if (contentType.Contains("application/json"))
                        {
                            request.Json(reader.ReadToEnd());
                        }
                        else if (contentType.Contains("application/xml"))
                        {
                            request.Xml(reader.ReadToEnd());
                        }
                        else if (contentType.Contains("application/x-www-form-urlencoded"))
                        {
                            request.Form(reader.ReadToEnd());
                        }
                        else
                        {
                            throw new NotImplementedException($"未实现({contentType})类型传输!");
                        }
                    }
                }

                try
                {
                    await context.Response.WriteAsync(await request.RequestAsync(context.Request.Method ?? "GET", "map:timeout".Config(10000)));
                }
                catch (WebException e)
                {
                    await context.Response.WriteJsonAsync(ExceptionHandler.Handler(e));
                }

            }));
        }
    }
}
#endif
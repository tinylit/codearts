#if !NET40
#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
#else
using CodeArts.Mvc.Builder;
using System.Web;
#endif
using CodeArts.Exceptions;
using System;
using System.IO;
using System.Net;

namespace CodeArts.Mvc
{
    /// <summary>
    /// 拓展
    /// </summary>
    public static class MapExtensions
    {
        /// <summary>
        /// 路由(HttpVerbs.Get)
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="destinationPath">获取目标地址</param>
        /// <returns></returns>
        public static IApplicationBuilder MapGet(this IApplicationBuilder app, PathString path, Func<PathString> destinationPath) => app.Map(path, HttpVerbs.GET, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.POST)
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="destinationPath">获取目标地址</param>
        /// <returns></returns>
        public static IApplicationBuilder MapPost(this IApplicationBuilder app, PathString path, Func<PathString> destinationPath) => app.Map(path, HttpVerbs.POST, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.PUT)
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="destinationPath">获取目标地址</param>
        /// <returns></returns>
        public static IApplicationBuilder MapPut(this IApplicationBuilder app, PathString path, Func<PathString> destinationPath) => app.Map(path, HttpVerbs.PUT, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.DELETE)
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="destinationPath">获取目标地址</param>
        /// <returns></returns>
        public static IApplicationBuilder MapDelete(this IApplicationBuilder app, PathString path, Func<PathString> destinationPath) => app.Map(path, HttpVerbs.DELETE, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.Get | HttpVerbs.Post | HttpVerbs.Put | HttpVerbs.Delete | HttpVerbs.Head | HttpVerbs.Patch | HttpVerbs.Options)
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="destinationPath">目标地址</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, PathString destinationPath) => app.Map(path, () => destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.Get | HttpVerbs.Post | HttpVerbs.Put | HttpVerbs.Delete | HttpVerbs.Head | HttpVerbs.Patch | HttpVerbs.Options)
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="destinationPath">获取目标地址</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, Func<PathString> destinationPath)
        {
            if (destinationPath is null)
            {
                throw new ArgumentNullException(nameof(destinationPath));
            }

            return app.Map(path, HttpVerbs.GET | HttpVerbs.POST | HttpVerbs.PUT | HttpVerbs.DELETE | HttpVerbs.HEAD | HttpVerbs.PATCH | HttpVerbs.OPTIONS, destinationPath);
        }

        /// <summary>
        /// 路由
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="httpVerbs">请求方式</param>
        /// <param name="destinationPath">目标地址</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, HttpVerbs httpVerbs, PathString destinationPath) => app.Map(path, httpVerbs, () => destinationPath);

        /// <summary>
        /// 路由
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="path">路由地址</param>
        /// <param name="httpVerbs">请求方式</param>
        /// <param name="destinationPath">获取目标地址</param>
        /// <returns></returns>

        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, HttpVerbs httpVerbs, Func<PathString> destinationPath)
        {
            if (destinationPath is null)
            {
                throw new ArgumentNullException(nameof(destinationPath));
            }

#if NETSTANDARD2_0 || NETCOREAPP3_1
            return app.Map(path, builder => builder.Run(async context =>
            {
                string method = context.Request.Method;
#else
            return app.Map(path, async (HttpContext context) =>
            {
                string method = context.Request.HttpMethod;
#endif

                if (!Enum.TryParse(method ?? "GET", true, out HttpVerbs verbs) || (httpVerbs & verbs) == 0)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                string destinationUrl = destinationPath.Invoke();

                if (string.IsNullOrEmpty(destinationUrl))
                {
                    context.Response.StatusCode = 404;

                    return;
                }

#if NETSTANDARD2_0 || NETCOREAPP3_1

                if (destinationUrl.IsUrl() ? !Uri.TryCreate(destinationUrl, UriKind.Absolute, out Uri uri) : !Uri.TryCreate($"{context.Request.Scheme}://{context.Request.Host}/{destinationUrl.TrimStart('/')}", UriKind.Absolute, out uri))
                {
                    await context.Response.WriteJsonAsync(DResult.Error($"不规范的接口({path.Value}=>{destinationUrl})!", StatusCodes.NonstandardServerError));
                    return;
                }
#else
                if (destinationUrl.IsUrl() ? !Uri.TryCreate(destinationUrl, UriKind.Absolute, out Uri uri) : !Uri.TryCreate($"{context.Request.Url.Scheme}://{context.Request.Url.Authority}/{destinationUrl.TrimStart('/')}", UriKind.Absolute, out uri))
                {
                    context.Response.WriteJson(DResult.Error($"不规范的接口({path.Value}=>{destinationUrl})!", StatusCodes.NonstandardServerError));
                    return;
                }
#endif

                var request = uri.AsRequestable().ToQueryString(context.Request.QueryString.ToString());

                var token = context.Request.Headers["Authorization"];

                if (!string.IsNullOrEmpty(token))
                {
                    request.AppendHeader("Authorization", token);
                }

                if (verbs != HttpVerbs.GET && verbs != HttpVerbs.DELETE)
                {
                    string contentType = context.Request.ContentType?.ToLower() ?? "application/json";

                    if (contentType.Contains("application/x-www-form-urlencoded"))
                    {
                        request.ToForm(context.Request.Form);
                    }
                    else
                    {

#if NETSTANDARD2_0 || NETCOREAPP3_1
                        using (var reader = new StreamReader(context.Request.Body))
#else
                        using (var reader = new StreamReader(context.Request.GetBufferedInputStream()))
#endif
                        {
                            if (contentType.Contains("application/json"))
                            {
                                request.ToJson(reader.ReadToEnd());
                            }
                            else if (contentType.Contains("application/xml"))
                            {
                                request.ToXml(reader.ReadToEnd());
                            }
                            else
                            {
                                throw new NotImplementedException($"未实现({contentType})类型传输!");
                            }
                        }
                    }
                }

#if NETSTANDARD2_0 || NETCOREAPP3_1
                try
                {
                    await context.Response.WriteAsync(await request.RequestAsync(method ?? "GET", "map:timeout".Config(10000)));
                }
                catch (WebException e)
                {
                    await context.Response.WriteJsonAsync(ExceptionHandler.Handler(e));
                }
            }));
#else
                try
                {
                    context.Response.Write(await request.RequestAsync(method ?? "GET", "map-timeout".Config(10000)));
                }
                catch (WebException e)
                {
                    context.Response.WriteJson(ExceptionHandler.Handler(e));
                }
            });
#endif
        }
    }
}
#endif
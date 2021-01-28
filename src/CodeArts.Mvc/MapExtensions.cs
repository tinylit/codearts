using System;
using System.Net;
using System.Text;
#if NET_CORE
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
#else
using System.IO;
using System.Web;
using CodeArts.Mvc.Builder;
#endif
using CodeArts.Exceptions;

namespace CodeArts.Mvc
{
    /// <summary>
    /// 拓展。
    /// </summary>
    public static class MapExtensions
    {
        /// <summary>
        /// 路由(HttpVerbs.Get)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapGet(this IApplicationBuilder app, PathString path, PathString destinationPath) => app.Map(path, HttpVerbs.GET, () => destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.Get)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">获取目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapGet(this IApplicationBuilder app, PathString path, Func<PathString> destinationPath) => app.Map(path, HttpVerbs.GET, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.POST)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapPost(this IApplicationBuilder app, PathString path, PathString destinationPath) => app.Map(path, HttpVerbs.POST, () => destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.POST)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">获取目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapPost(this IApplicationBuilder app, PathString path, Func<PathString> destinationPath) => app.Map(path, HttpVerbs.POST, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.PUT)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapPut(this IApplicationBuilder app, PathString path, PathString destinationPath) => app.Map(path, HttpVerbs.PUT, () => destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.PUT)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">获取目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapPut(this IApplicationBuilder app, PathString path, Func<PathString> destinationPath) => app.Map(path, HttpVerbs.PUT, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.DELETE)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapDelete(this IApplicationBuilder app, PathString path, PathString destinationPath) => app.Map(path, HttpVerbs.DELETE, () => destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.DELETE)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">获取目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapDelete(this IApplicationBuilder app, PathString path, Func<PathString> destinationPath) => app.Map(path, HttpVerbs.DELETE, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.Get | HttpVerbs.Post | HttpVerbs.Put | HttpVerbs.Delete | HttpVerbs.Head | HttpVerbs.Patch | HttpVerbs.Options)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, PathString destinationPath) => app.Map(path, () => destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.Get | HttpVerbs.Post | HttpVerbs.Put | HttpVerbs.Delete | HttpVerbs.Head | HttpVerbs.Patch | HttpVerbs.Options)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">获取目标地址。</param>
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
        /// 路由。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="httpVerbs">请求方式。</param>
        /// <param name="destinationPath">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, HttpVerbs httpVerbs, PathString destinationPath) => app.Map(path, httpVerbs, () => destinationPath);

        /// <summary>
        /// 路由。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="httpVerbs">请求方式。</param>
        /// <param name="destinationPath">获取目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, HttpVerbs httpVerbs, Func<PathString> destinationPath)
        {
            if (destinationPath is null)
            {
                throw new ArgumentNullException(nameof(destinationPath));
            }

#if NET_CORE
            return app.Map(path, builder => builder.Run(async context =>
            {
                string method = context.Request.Method;
#elif NET40
            return app.Map(path, (HttpContext context) =>
            {
                string method = context.Request.HttpMethod;
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

#if NET_CORE

                if (destinationUrl.IsUrl() ? !Uri.TryCreate(destinationUrl, UriKind.Absolute, out Uri uri) : !Uri.TryCreate($"{context.Request.Scheme}://{context.Request.Host}/{destinationUrl.TrimStart('/')}", UriKind.Absolute, out uri))
                {
                    await context.Response.WriteJsonAsync(DResult.Error($"不规范的接口({path.Value}=>{destinationUrl})!", StatusCodes.NonstandardServerError)).ConfigureAwait(false);
                    
                    return;
                }
#else
                if (destinationUrl.IsUrl() ? !Uri.TryCreate(destinationUrl, UriKind.Absolute, out Uri uri) : !Uri.TryCreate($"{context.Request.Url.Scheme}://{context.Request.Url.Authority}/{destinationUrl.TrimStart('/')}", UriKind.Absolute, out uri))
                {
                    context.Response.WriteJson(DResult.Error($"不规范的接口({path.Value}=>{destinationUrl})!", StatusCodes.NonstandardServerError));
                    return;
                }
#endif

                var request = uri.AsRequestable().AppendQueryString(context.Request.QueryString.ToString());

                var token = context.Request.Headers["Authorization"];

                if (!string.IsNullOrEmpty(token))
                {
                    request.AssignHeader("Authorization", token);
                }

                string contentType = context.Request.ContentType?.ToLower() ?? string.Empty;

                if (contentType.Contains("application/x-www-form-urlencoded"))
                {
                    request.Form(context.Request.Form);
                }
                else if (contentType.IsNotEmpty())
                {
#if NET_CORE
                    if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > 0)
                    {
                        var length = context.Request.ContentLength.Value;
                        var buffer = new byte[length];
                        await context.Request.Body.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                        var body = Encoding.UTF8.GetString(buffer);

                        request.Body(body, contentType);
                    }
#else
                    if (context.Request.InputStream.Length > 0)
                    {
                        using (var reader = new StreamReader(context.Request.InputStream))
                        {
                            request.Body(reader.ReadToEnd(), contentType);
                        }
                    }
#endif
                }

#if NET_CORE
                try
                {
                    await context.Response.WriteAsync(await request.RequestAsync(method ?? "GET", "map:timeout".Config(Consts.MapTimeout)).ConfigureAwait(false));
                }
                catch (WebException e)
                {
                    await context.Response.WriteJsonAsync(ExceptionHandler.Handler(e)).ConfigureAwait(false);
                }
            }));
#else
                try
                {
#if NET40
                    context.Response.Write(request.Request(method ?? "GET", "map-timeout".Config(Consts.MapTimeout)));
#else
                    context.Response.Write(await request.RequestAsync(method ?? "GET", "map-timeout".Config(Consts.MapTimeout)).ConfigureAwait(false));
#endif
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

using System;
using System.Net;
using System.Threading.Tasks;
#if NETCOREAPP2_0_OR_GREATER
using System.Text;
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
        public static IApplicationBuilder MapGet(this IApplicationBuilder app, PathString path, PathString destinationPath) => app.Map(path, HttpVerbs.GET, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.POST)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapPost(this IApplicationBuilder app, PathString path, PathString destinationPath) => app.Map(path, HttpVerbs.POST, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.PUT)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapPut(this IApplicationBuilder app, PathString path, PathString destinationPath) => app.Map(path, HttpVerbs.PUT, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.DELETE)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapDelete(this IApplicationBuilder app, PathString path, PathString destinationPath) => app.Map(path, HttpVerbs.DELETE, destinationPath);

        /// <summary>
        /// 路由(HttpVerbs.Get | HttpVerbs.Post | HttpVerbs.Put | HttpVerbs.Delete | HttpVerbs.Head | HttpVerbs.Patch | HttpVerbs.Options)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationPath">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, PathString destinationPath)
            => app.Map(path, HttpVerbs.GET | HttpVerbs.POST | HttpVerbs.PUT | HttpVerbs.DELETE | HttpVerbs.HEAD | HttpVerbs.PATCH | HttpVerbs.OPTIONS, destinationPath);

#if NET40
        private static void Map(HttpContext context, Uri destinationUri)
#else
        private static async Task Map(HttpContext context, Uri destinationUri)
#endif
        {
            var request = destinationUri.AsRequestable()
            .AppendQueryString(context.Request.QueryString.ToString());

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
#if NETCOREAPP2_0_OR_GREATER
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

#if NETCOREAPP2_0_OR_GREATER
            string method = context.Request.Method;
#else
            string method = context.Request.HttpMethod;
#endif

#if NETCOREAPP2_0_OR_GREATER
            try
            {
                await context.Response.WriteAsync(await request.RequestAsync(method ?? "GET", "map:timeout".Config(Consts.MapTimeout)).ConfigureAwait(false));
            }
            catch (WebException e)
            {
                await context.Response.WriteJsonAsync(ExceptionHandler.Handler(e)).ConfigureAwait(false);
            }
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
#endif
        }

        /// <summary>
        /// 路由。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="httpVerbs">请求方式。</param>
        /// <param name="destinationPath">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, HttpVerbs httpVerbs, PathString destinationPath)
        {
            return app.Map(path, httpVerbs, context => Map(context, new Uri(destinationPath.HasValue
#if NETCOREAPP2_0_OR_GREATER
                    ? $"{context.Request.Scheme}://{context.Request.Host}{destinationPath.Value}"
                    : $"{context.Request.Scheme}://{context.Request.Host}/"
#else
                    ? $"{context.Request.Url.Scheme}://{context.Request.Url.Host}{destinationPath.Value}"
                    : $"{context.Request.Url.Scheme}://{context.Request.Url.Host}/"
#endif
                    )));
        }

        /// <summary>
        /// 路由(HttpVerbs.Get)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationUri">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapGet(this IApplicationBuilder app, PathString path, Uri destinationUri) => app.Map(path, HttpVerbs.GET, destinationUri);

        /// <summary>
        /// 路由(HttpVerbs.POST)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationUri">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapPost(this IApplicationBuilder app, PathString path, Uri destinationUri) => app.Map(path, HttpVerbs.POST, destinationUri);

        /// <summary>
        /// 路由(HttpVerbs.PUT)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationUri">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapPut(this IApplicationBuilder app, PathString path, Uri destinationUri) => app.Map(path, HttpVerbs.PUT, destinationUri);

        /// <summary>
        /// 路由(HttpVerbs.DELETE)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationUri">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder MapDelete(this IApplicationBuilder app, PathString path, Uri destinationUri) => app.Map(path, HttpVerbs.DELETE, destinationUri);

        /// <summary>
        /// 路由(HttpVerbs.Get | HttpVerbs.Post | HttpVerbs.Put | HttpVerbs.Delete | HttpVerbs.Head | HttpVerbs.Patch | HttpVerbs.Options)。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="destinationUri">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, Uri destinationUri)
            => app.Map(path, HttpVerbs.GET | HttpVerbs.POST | HttpVerbs.PUT | HttpVerbs.DELETE | HttpVerbs.HEAD | HttpVerbs.PATCH | HttpVerbs.OPTIONS, destinationUri);

        /// <summary>
        /// 路由。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="httpVerbs">请求方式。</param>
        /// <param name="destinationUri">目标地址。</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, HttpVerbs httpVerbs, Uri destinationUri)
        {
            if (destinationUri is null)
            {
                throw new ArgumentNullException(nameof(destinationUri));
            }

            return app.Map(path, httpVerbs, context => Map(context, destinationUri));
        }

        /// <summary>
        /// 路由。
        /// </summary>
        /// <param name="app">app。</param>
        /// <param name="path">路由地址。</param>
        /// <param name="httpVerbs">请求方式。</param>
        /// <param name="handler">请求。</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, HttpVerbs httpVerbs, RequestDelegate handler)
        {
#if NETCOREAPP3_1_OR_GREATER
            if (path.HasValue && path.Value[^1] == '/')
#else
            if (path.HasValue && path.Value[path.Value.Length - 1] == '/')
#endif
            {
                path = new PathString(path.Value.TrimEnd('/'));
            }

#if NETCOREAPP2_1_OR_GREATER
            return app.Map(path, x => x.Use(next => context =>
            {
                if (!Enum.TryParse(context.Request.Method ?? "GET", true, out HttpVerbs verbs) || (httpVerbs & verbs) == 0)
                {
                    return next.Invoke(context);
                }

                PathString absolutePath = context.Request.PathBase.Add(context.Request.Path);

                if (absolutePath.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase, out PathString segments) && !segments.HasValue)
                {
                    return handler.Invoke(context);
                }

                return next.Invoke(context);
            }));
#elif NET40
            return app.Use(next => context =>
            {
                if (!Enum.TryParse(context.Request.HttpMethod ?? "GET", true, out HttpVerbs verbs) || (httpVerbs & verbs) == 0)
                {
                    next.Invoke(context);
                }
                else
                {
                    PathString absolutePath = new PathString(context.Request.Url.AbsolutePath);
                    
                    if (absolutePath.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase, out PathString segments) && !segments.HasValue)
                    {
                        handler.Invoke(context);
                    }
                    else
                    {
                        next.Invoke(context);
                    }
                }
            });
#else
            return app.Use(next => context =>
            {
                if (!Enum.TryParse(context.Request.HttpMethod ?? "GET", true, out HttpVerbs verbs) || (httpVerbs & verbs) == 0)
                {
                    return next.Invoke(context);
                }

                PathString absolutePath = new PathString(context.Request.Url.AbsolutePath);

                if (absolutePath.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase, out PathString segments) && !segments.HasValue)
                {
                    return handler.Invoke(context);
                }

                return next.Invoke(context);
            });
#endif
        }
    }
}

#if NET40 || NET45 || NET451 || NET452 || NET461
using System;

namespace SkyBuilding.Mvc.Builder
{
    /// <summary>
    /// 程序构建器扩展
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// 路径映射
        /// </summary>
        /// <param name="app">程序构建器</param>
        /// <param name="path">映射路径</param>
        /// <param name="request">请求</param>
        /// <returns></returns>
        public static IApplicationBuilder Map(this IApplicationBuilder app, PathString path, RequestDelegate request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return app.Use(next =>
            {
                return context =>
                {
                    PathString absolutePath = new PathString(context.Request.Url.AbsolutePath);

                    if (path.Equals(absolutePath, StringComparison.OrdinalIgnoreCase) || absolutePath.StartsWithSegments(path, StringComparison.OrdinalIgnoreCase))
                    {
#if NET40
                        request.Invoke(context);
                    }
                    else
                    {
                        next.Invoke(context);
                    }
#else
                        return request.Invoke(context);
                    }
                    return next.Invoke(context);
#endif
                };
            });
        }
    }
}
#endif
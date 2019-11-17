#if NET40
using Newtonsoft.Json.Serialization;
using SkyBuilding;
using SkyBuilding.Cache;
using SkyBuilding.Config;
using SkyBuilding.Mvc;
using SkyBuilding.Mvc.Converters;
using SkyBuilding.Serialize.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http.Routing;
using System.Web.Routing;

namespace System.Web.Http
{
    /// <summary>
    /// 注册配置
    /// </summary>
    public static class RegistorHttpConfigurationExtentions
    {
        /// <summary>
        /// 属性名称解析规则
        /// </summary>
        private class ContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName) => propertyName.ToCamelCase();
        }

        private class HttpRouteConstraint : IHttpRouteConstraint, IRouteConstraint
        {
            public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection)
            {
                string path = request.RequestUri.AbsolutePath.Trim('/');

                return request.Method == HttpMethod.Get && (string.Equals(path, "login", StringComparison.OrdinalIgnoreCase) || string.Equals(path, "authCode", StringComparison.OrdinalIgnoreCase));
            }

            public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
            {
                string path = httpContext.Request.Url.AbsolutePath.Trim('/');

                return (string.Equals(path, "login", StringComparison.OrdinalIgnoreCase) || string.Equals(path, "authCode", StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// 注册（路由、异常捕获、JSON转换器、JSON解析器、配置文件助手、日志服务）
        /// </summary>
        /// <param name="config">协议配置</param>
        public static HttpConfiguration Register(this HttpConfiguration config)
        {
            //? 注册登录路由
            config.Routes.MapHttpRoute(
                name: "route",
                routeTemplate: "{controller}",
                defaults: new { debug = RouteParameter.Optional },
                constraints: new { debug = new HttpRouteConstraint() }
            );

            //? 注册默认路由
            config.Routes.MapHttpRoute(
                name: "controller",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.Routes.MapHttpRoute(
                name: "action",
                routeTemplate: "api/{controller}/{aciton}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            //?天空之城JSON转换器（修复长整型前端数据丢失的问题）
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            config.Formatters
                .JsonFormatter
                .SerializerSettings
                .PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None;

            config.Formatters
                .JsonFormatter
                .SerializerSettings
                .DateFormatString = Consts.DateFormatString;

            config.Formatters
                .JsonFormatter
                .SerializerSettings
                .Converters
                .Add(new SkyJsonConverter());

            config.Formatters
                .JsonFormatter
                .SerializerSettings
                .ContractResolver = new ContractResolver();

            //?异常捕获
            config.Filters.Add(new DExceptionFilterAttribute());

            //? JSON解析器
            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();
            RuntimeServManager.TryAddSingleton<IConfigHelper, DefaultConfigHelper>();

            //? 缓存服务
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.First);
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.Second);

            return config;
        }
    }
}
#endif
#if NET40 || NET45 || NET451 || NET452 || NET461
#if NET45 || NET451 || NET452 || NET461
using Autofac;
using Autofac.Integration.WebApi;
using Swashbuckle.Application;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Net;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
#else
using System.Collections.Generic;
using System.Web.Http.Routing;
using System.Net.Http;
using System.Web.Routing;
using System.Web;
#endif
using SkyBuilding.Config;
using SkyBuilding.Log;
using SkyBuilding.Serialize.Json;
using System;
using System.Web.Http;
using Newtonsoft.Json.Serialization;
using SkyBuilding.Cache;
using SkyBuilding.Converters;

namespace SkyBuilding.Mvc
{
    /// <summary>
    /// 启动类
    /// </summary>
    public static class ApiConfig
    {
        /// <summary>
        /// 属性名称解析规则
        /// </summary>
        private class ContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                return propertyName.ToCamelCase();
            }
        }

#if NET40
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
#endif
        /// <summary>
        /// 注册（路由、异常捕获、JSON转换器、JSON解析器、配置文件助手、日志服务、依赖注入）
        /// </summary>
        /// <param name="config">协议配置</param>
        public static void Register(HttpConfiguration config)
        {
#if NET40
            //? 注册登录路由
            config.Routes.MapHttpRoute(
                name: "route",
                routeTemplate: "{controller}",
                defaults: new { },
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

#else
            // Web API 路由
            config.MapHttpAttributeRoutes();

            config.Routes.IgnoreRoute("ignore", "{resource}.axd/{*pathInfo}");

            //? 注册默认路由
            config.Routes.Add("route", config.Routes.CreateRoute("api/{controller}/{id}", new { id = RouteParameter.Optional }, new object()));
#endif
            //?天空之城JSON转换器（修复长整型前端数据丢失的问题）
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

            //? 日志服务
            LogManager.AddAdapter(new Log4NetAdapter());

            //? 缓存服务
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.First);
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.Second);

#if NET45 || NET451 || NET452 || NET461
            //? 依赖注入
            config.DependencyResolver = new AutofacWebApiDependencyResolver(IocRegisters(new ContainerBuilder()));
#else
            config.DependencyResolver = new SkyDependencyResolver();
#endif
        }

#if NET45 || NET451 || NET452 || NET461

        /// <summary>
        /// 配置SwaggerUI
        /// </summary>
        /// <param name="config">配置</param>
        public static void SwaggerUI(HttpConfiguration config)
        {
            config.EnableSwagger(c =>
            {
                c.Schemes(new[] { "http", "https" });

                c.SingleApiVersion("swagger-version".Config("v1"), "swagger-title".Config("API接口文档"));

                var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    c.IncludeXmlComments(file);
                }

                if (Directory.Exists(AppDomain.CurrentDomain.RelativeSearchPath))
                {
                    var relativeFiles = Directory.GetFiles(AppDomain.CurrentDomain.RelativeSearchPath, "*.xml", SearchOption.TopDirectoryOnly);
                    foreach (var file in relativeFiles)
                    {
                        c.IncludeXmlComments(file);
                    }
                }

                c.IgnoreObsoleteProperties();

                c.DescribeAllEnumsAsStrings();

                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

            }).EnableSwaggerUi(c =>
            {
                c.DocExpansion(DocExpansion.List);
            });
        }

        #region Private
        /// <summary>
        /// 依赖注入
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        private static IContainer IocRegisters(ContainerBuilder builder)
        {
            var path = AppDomain.CurrentDomain.RelativeSearchPath;
            if (!Directory.Exists(path))
                path = AppDomain.CurrentDomain.BaseDirectory;

            var assemblys = Directory.GetFiles(path, "*.dll")
                    .Select(Assembly.LoadFrom)
                    .SelectMany(x => x.GetTypes());

            var controllerTypes = assemblys
                .Where(type => type.IsClass && !type.IsAbstract && typeof(ApiController).IsAssignableFrom(type));

            var interfaceTypes = controllerTypes
                .SelectMany(type =>
                {
                    return type.GetConstructors()
                          .SelectMany(x => x.GetParameters()
                          .Select(y => y.ParameterType));
                }).Distinct();

            var types = interfaceTypes.SelectMany(x => assemblys.Where(y => x.IsAssignableFrom(y)));

            builder.RegisterTypes(types.Union(interfaceTypes).Union(controllerTypes).ToArray())
                .Where(type => type.IsInterface || type.IsClass)
                .AsSelf() //自身服务，用于没有接口的类
                .AsImplementedInterfaces() //接口服务
                .PropertiesAutowired(); //属性注入

            return builder.Build();
        }
        #endregion
#endif
    }
}
#endif
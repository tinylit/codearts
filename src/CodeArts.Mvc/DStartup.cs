#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using CodeArts.Cache;
using CodeArts.Config;
using CodeArts.Serialize.Json;
using CodeArts.Mvc.Converters;
using Microsoft.Extensions.Logging;
#if NETCOREAPP3_1
using Microsoft.Extensions.Hosting;
#else
using System;
using System.IO;
using Swashbuckle.AspNetCore.Swagger;
#endif

namespace CodeArts.Mvc
{
    /// <summary>
    /// 启动基类
    /// </summary>
    public class DStartup
    {
        /// <summary>
        /// 是否使用SwaggerUi。（默认：true）
        /// </summary>
        protected bool UseSwaggerUi { get; set; } = true;

        /// <summary>
        /// 使用依赖注入<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/>(默认:true)。
        /// </summary>
        protected bool UseDependencyInjection { get; set; } = true;

        /// <summary>
        /// 配置服务（这个方法被运行时调用。使用此方法向容器添加服务。）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns></returns>

        public virtual void ConfigureServices(IServiceCollection services)
        {
#if NETCOREAPP3_1
            services.AddControllers();

            ConfigureMvc(services
                  .AddMvc(options =>
                  {
                      options.EnableEndpointRouting = false;
                      //自定义异常捕获
                      options.Filters.Add<DExceptionFilter>();
                  })
                  .AddJsonOptions(options =>
                  {
                      options.JsonSerializerOptions.Converters.Add(new SkyJsonConverter());
                  })
                  .SetCompatibilityVersion(CompatibilityVersion.Version_3_0));
#else
            ConfigureMvc(services
                 .AddMvc(options =>
                 {
                     //自定义异常捕获
                     options.Filters.Add<DExceptionFilter>();
                 })
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.DateFormatString = Consts.DateFormatString;
                    options.SerializerSettings.Converters.Add(new SkyJsonConverter());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2));
#endif

            services.AddCors(options =>
            {
                options.AddPolicy("Allow",
                    builder =>
                    {
                        builder.SetIsOriginAllowed(origin => true)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    });
            });

            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();
            RuntimeServManager.TryAddSingleton<IConfigHelper, DefaultConfigHelper>();

            //? 缓存服务
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.First);
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.Second);

            if (UseDependencyInjection)
            {
                services.UseDependencyInjection();
            }
            if (UseSwaggerUi)
            {
#if NETCOREAPP3_1
                //增加XML文档解析
                //services.AddSwaggerGen(c =>
                //{
                //    c.SwaggerDoc("swagger:version".Config(Consts.SwaggerVersion), new OpenApiInfo { Title = "swagger:title".Config(Consts.SwaggerTitle), Version = "v3" });
                //
                //    var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
                //    foreach (var file in files)
                //    {
                //        c.IncludeXmlComments(file);
                //    }
                //});
#else
                //增加XML文档解析
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("swagger:version".Config(Consts.SwaggerVersion), new Info { Title = "swagger:title".Config(Consts.SwaggerTitle), Version = "v3" });

                    var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        c.IncludeXmlComments(file);
                    }
                });
#endif
            }
        }

        /// <summary>
        /// 配置<see cref="IMvcBuilder"/>通道，可添加Xml等支持。
        /// </summary>
        /// <param name="builder">MVC构造器</param>
        protected virtual IMvcBuilder ConfigureMvc(IMvcBuilder builder) => builder;

#if NETCOREAPP3_1
        /// <summary>
        /// 配置管道（此方法由运行时调用。使用此方法配置HTTP请求管道。）
        /// </summary>
        /// <param name="app">项目构建器</param>
        /// <param name="env">环境变量</param>
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
#else
        /// <summary>
        /// 配置管道
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
#endif
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

#if NETCOREAPP3_1
            //? 跨域
            app.UseStaticFiles()
                .UseCors("Allow")
                .UseRouting()
                .UseMvc()
                .UseLoggerManager();

            if (UseSwaggerUi)
            {
                //app.UseSwagger()
                //    .UseSwaggerUI(c =>
                //    {
                //        c.SwaggerEndpoint("/swagger/" + "swagger:version".Config(Consts.SwaggerVersion) + "/swagger.json", "swagger:title".Config(Consts.SwaggerTitle));
                //    })
                //    .UseEndpoints(endpoints =>
                //    {
                //        endpoints.MapControllers();
                //    });
            }
#else
            //? 跨域
            app.UseStaticFiles()
                .UseCors("Allow")
                .UseMvc()
                .UseLoggerManager();

            if (UseSwaggerUi)
            {
                app.UseSwagger()
                    .UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/" + "swagger:version".Config(Consts.SwaggerVersion) + "/swagger.json", "swagger:title".Config(Consts.SwaggerTitle));
                    });
            }
#endif
        }
    }
}
#else
            using Newtonsoft.Json.Serialization;
using CodeArts.Cache;
using CodeArts.Config;
using CodeArts.Mvc.Builder;
using CodeArts.Mvc.Converters;
using CodeArts.Mvc.DependencyInjection;
using CodeArts.Serialize.Json;
#if NET45 || NET451 || NET452 || NET461
using Swashbuckle.Application;
using System.IO;
using System.Linq;
#endif
using System;
using System.Web.Http;
using System.Web.Http.Dependencies;

namespace CodeArts.Mvc
{
    /// <summary>
    /// 启动基类。
    /// 运行顺序：
    /// Configuration: 配置请求内容。（可支持返回值类型:<see cref="void"/>,<see cref="IServiceProvider"/>,<see cref="IDependencyResolver"/>）。
    /// ConfigureServices:配置依赖注入。（忽略返回值，语法:ConfigureServices(<see cref="IServiceCollection"/>)）。
    /// Configure:配置请求管道，配置请求中间件。(忽略返回值，语法:Configure(<see cref="IApplicationBuilder"/>))。
    /// </summary>
    public class DStartup
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
#if NET45 || NET451 || NET452 || NET461
        /// <summary>
        /// 是否使用SwaggerUi（默认：true）
        /// </summary>
        protected bool UseSwaggerUi { get; set; } = true;
#endif

        /// <summary>
        /// 使用依赖注入<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/>(默认:true)。
        /// </summary>
        protected bool UseDependencyInjection { get; set; } = true;

        /// <summary>
        /// 配置默认路由规则，解析器等。
        /// </summary>
        /// <param name="config">配置</param>
        public virtual void Configuration(HttpConfiguration config)
        {
#if NET45 || NET451 || NET452 || NET461
            // Web API 路由
            config.MapHttpAttributeRoutes();

            config.Routes.IgnoreRoute("ignore", "{resource}.axd/{*pathInfo}");
#endif

            //? 注册默认路由
            config.Routes.Add("route", config.Routes.CreateRoute("api/{controller}/{id}", new { id = RouteParameter.Optional }, new object()));

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

#if NET45 || NET451 || NET452 || NET461
            if (UseSwaggerUi)
            {
                //? SwaggerUi
                config.EnableSwagger(c =>
                {
                    c.Schemes(new[] { "http", "https" });

                    c.SingleApiVersion("swagger-version".Config(Consts.SwaggerVersion), "swagger-title".Config(Consts.SwaggerTitle));

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
#endif

            //?异常捕获
            config.Filters.Add(new DExceptionFilterAttribute());

            //? JSON解析器
            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();
            RuntimeServManager.TryAddSingleton<IConfigHelper, DefaultConfigHelper>();

            //? 缓存服务
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.First);
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.Second);
        }

        /// <summary>
        /// 配置依赖注入。
        /// </summary>
        /// <param name="services">服务集合</param>
        public virtual void ConfigureServices(IServiceCollection services)
        {
            if (UseDependencyInjection)
            {
                services.UseDependencyInjection();
            }
        }
    }
}
#endif
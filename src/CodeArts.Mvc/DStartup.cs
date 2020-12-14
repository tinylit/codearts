#if NET_CORE
using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using CodeArts.Caching;
using CodeArts.Config;
using CodeArts.Serialize.Json;
using CodeArts.Mvc.Converters;
using Microsoft.Extensions.Logging;
using CodeArts.Mvc.Validators.DataAnnotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
#if NETCOREAPP3_1
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Hosting;
#else
using Swashbuckle.AspNetCore.Swagger;
#endif

namespace CodeArts.Mvc
{
    /// <summary>
    /// 启动基类。
    /// </summary>
    public class DStartup
    {
        private readonly bool useSwaggerUi;
        private readonly bool useDependencyInjection;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="useSwaggerUi">使用SwaggerUi。</param>
        /// <param name="useDependencyInjection">使用依赖注入<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/>。</param>
        public DStartup(bool useSwaggerUi = true, bool useDependencyInjection = true)
        {
            this.useSwaggerUi = useSwaggerUi;
            this.useDependencyInjection = useDependencyInjection;
        }

        /// <summary>
        /// 配置服务（这个方法被运行时调用。使用此方法向容器添加服务）。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns></returns>

        public virtual void ConfigureServices(IServiceCollection services)
        {
            ConfigureMvc(services);

            services.AddCors(options =>
            {
                options.AddPolicy("Allow",
                    builder =>
                    {
                        builder.SetIsOriginAllowed(origin => true)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });

            //? 缓存服务
            CachingManager.RegisterProvider(new MemoryCachingProvider(), Level.First);

            if (useDependencyInjection)
            {
                services.UseDependencyInjection();
            }

            if (useSwaggerUi)
            {
                //增加XML文档解析
                services.AddSwaggerGen(ConfigureSwaggerGen);
            }
        }

        /// <summary>
        /// 配置SwaggerGen。
        /// </summary>
        /// <param name="options">SwaggerGen配置项。</param>
        protected virtual void ConfigureSwaggerGen(SwaggerGenOptions options)
        {
#if NETCOREAPP3_1
            options.SwaggerDoc("swagger:version".Config(Consts.SwaggerVersion), new OpenApiInfo { Title = "swagger:title".Config(Consts.SwaggerTitle), Version = "v3" });
#else
            options.SwaggerDoc("swagger:version".Config(Consts.SwaggerVersion), new Info { Title = "swagger:title".Config(Consts.SwaggerTitle), Version = "v3" });
#endif

            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                options.IncludeXmlComments(file);
            }

            options.IgnoreObsoleteActions();

            options.IgnoreObsoleteProperties();

            options.CustomSchemaIds(x => x.FullName);
        }

        /// <summary>
        /// 配置SwaggerUI。
        /// </summary>
        /// <param name="options">SwaggerUI配置项。</param>
        protected virtual void ConfigureSwaggerUI(SwaggerUIOptions options)
        {
            options.SwaggerEndpoint("/swagger/" + "swagger:version".Config(Consts.SwaggerVersion) + "/swagger.json", "swagger:title".Config(Consts.SwaggerTitle));
        }

        /// <summary>
        /// 配置MVC【<see cref="MvcServiceCollectionExtensions.AddMvc(IServiceCollection)"/>或<seealso cref="MvcServiceCollectionExtensions.AddMvc(IServiceCollection, Action{MvcOptions})"/>】。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns></returns>
#if NETCOREAPP3_1
        protected virtual IMvcBuilder ConfigureMvc(IServiceCollection services)
        {
            services.AddControllers();

            return services
                   .AddMvc(options =>
                   {
                       options.EnableEndpointRouting = false;

                       options.ModelValidatorProviders.Add(new DataAnnotationsModelValidatorProvider());

                       //自定义异常捕获
                       options.Filters.Add<DExceptionFilter>();
                   })
                   .AddJsonOptions(options =>
                   {
                       options.JsonSerializerOptions.IgnoreNullValues = true;
#if NETCOREAPP3_1
                       options.JsonSerializerOptions.Converters.Add(new MyJsonConverterFactory());
#endif
                       options.JsonSerializerOptions.Converters.Add(new MyJsonConverter());
                   })
                   .SetCompatibilityVersion(CompatibilityVersion.Latest);
        }
        
        /// <summary>
        /// 使用端点。
        /// </summary>
        /// <param name="endpoints">端点路由构造器。</param>
        protected virtual void UseEndpoints(IEndpointRouteBuilder endpoints) => endpoints
                    .MapControllers()
                    .RequireCors("Allow");
#else
        protected virtual IMvcBuilder ConfigureMvc(IServiceCollection services)
            => services
                .AddMvc(options =>
                {
                    options.ModelValidatorProviders.Add(new DataAnnotationsModelValidatorProvider());

                    //自定义异常捕获
                    options.Filters.Add<DExceptionFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.DateFormatString = Consts.DateFormatString;
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    options.SerializerSettings.Converters.Add(new MyJsonConverter());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
#endif

#if NETCOREAPP3_1
        /// <summary>
        /// 配置管道（此方法由运行时调用。使用此方法配置HTTP请求管道）。
        /// </summary>
        /// <param name="app">项目构建器。</param>
        /// <param name="env">环境变量。</param>
        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
#else
        /// <summary>
        /// 配置管道。
        /// </summary>
        /// <param name="app">程序构建器。</param>
        /// <param name="env">环境变量。</param>
        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
#endif
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

#if NETCOREAPP3_1
            //? 跨域
            app.UseCors("Allow")
                .UseRouting()
                .UseMvc()
                .UseLoggerManager()
                .UseEndpoints(UseEndpoints);

            if (useSwaggerUi)
            {
                app.UseSwagger()
                    .UseSwaggerUI(ConfigureSwaggerUI);
            }
#else
            //? 跨域
            app.UseCors("Allow")
                .UseMvc()
                .UseLoggerManager();

            if (useSwaggerUi)
            {
                app.UseSwagger()
                    .UseSwaggerUI(ConfigureSwaggerUI);
            }
#endif
        }
    }
}
#else
using Newtonsoft.Json.Serialization;
using CodeArts.Caching;
using CodeArts.Mvc.Builder;
using CodeArts.Mvc.Converters;
using CodeArts.Mvc.DependencyInjection;
#if NET_NORMAL
using Swashbuckle.Application;
using System.IO;
#endif
using System;
using System.Web.Http;
using System.Web.Http.Dependencies;
using System.Web.Http.Metadata;
using CodeArts.Mvc.Providers;

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

        private readonly bool useDependencyInjection;

#if NET40
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="useDependencyInjection">使用依赖注入<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/>。</param>
        public DStartup(bool useDependencyInjection = true)
        {
            this.useDependencyInjection = useDependencyInjection;
        }
#else
        private readonly bool useSwaggerUi;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="useSwaggerUi">使用SwaggerUi。</param>
        /// <param name="useDependencyInjection">使用依赖注入<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/>。</param>
        public DStartup(bool useSwaggerUi = true, bool useDependencyInjection = true)
        {
            this.useSwaggerUi = useSwaggerUi;
            this.useDependencyInjection = useDependencyInjection;
        }
#endif

        /// <summary>
        /// 属性名称解析规则。
        /// </summary>
        private class ContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                return propertyName.ToCamelCase();
            }
        }

        /// <summary>
        /// 配置默认路由规则，解析器等。
        /// </summary>
        /// <param name="config">配置。</param>
        public virtual void Configuration(HttpConfiguration config)
        {
#if !NET40
            // Web API 路由
            config.MapHttpAttributeRoutes();

            config.Routes.IgnoreRoute("ignore", "{resource}.axd/{*pathInfo}");
#endif
            //? 注册默认路由
            config.Routes.Add("code-arts", config.Routes.CreateRoute("api/{controller}/{id}", new { id = RouteParameter.Optional }, new object()));

            //?天空之城JSON转换器（修复长整型前端数据丢失的问题）
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            config.Formatters
                .JsonFormatter
                .SerializerSettings
                .PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None;

            config.Formatters
                .JsonFormatter
                .SerializerSettings
                .NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;

            config.Formatters
                .JsonFormatter
                .SerializerSettings
                .DateFormatString = Consts.DateFormatString;

            config.Formatters
                .JsonFormatter
                .SerializerSettings
                .Converters
                .Add(new MyJsonConverter());

            config.Formatters
                .JsonFormatter
                .SerializerSettings
                .ContractResolver = new ContractResolver();

            config.Services.Replace(typeof(ModelMetadataProvider), new DataAnnotationsModelMetadataProvider(config.Services.GetService(typeof(ModelMetadataProvider)) as System.Web.Http.Metadata.Providers.DataAnnotationsModelMetadataProvider));

#if !NET40
            if (useSwaggerUi)
            {
                //? SwaggerUi
                config.EnableSwagger(ConfigureSwagger)
                    .EnableSwaggerUi(ConfigureSwaggerUi);
            }
#endif

            //?异常捕获
            config.Filters.Add(new DExceptionFilterAttribute());

            //? 缓存服务
            CachingManager.RegisterProvider(new MemoryCachingProvider(), Level.First);
        }

#if !NET40
        /// <summary>
        /// 配置Swagger。
        /// </summary>
        /// <param name="config">Swagger文档配置项。</param>
        protected virtual void ConfigureSwagger(SwaggerDocsConfig config)
        {
            config.Schemes(new[] { "http", "https" });

            config.SingleApiVersion("swagger-version".Config(Consts.SwaggerVersion), "swagger-title".Config(Consts.SwaggerTitle));

            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                config.IncludeXmlComments(file);
            }

            if (Directory.Exists(AppDomain.CurrentDomain.RelativeSearchPath))
            {
                var relativeFiles = Directory.GetFiles(AppDomain.CurrentDomain.RelativeSearchPath, "*.xml", SearchOption.TopDirectoryOnly);
                foreach (var file in relativeFiles)
                {
                    config.IncludeXmlComments(file);
                }
            }

            config.IgnoreObsoleteActions();

            config.IgnoreObsoleteProperties();

            config.DescribeAllEnumsAsStrings();

            config.UseFullTypeNameInSchemaIds();
        }

        /// <summary>
        /// 配置SwaggerUi。
        /// </summary>
        /// <param name="config">SwaggerUi配置项。</param>
        protected virtual void ConfigureSwaggerUi(SwaggerUiConfig config) => config.DocExpansion(DocExpansion.List);

#endif

        /// <summary>
        /// 配置依赖注入。
        /// </summary>
        /// <param name="services">服务集合。</param>
        public virtual void ConfigureServices(IServiceCollection services)
        {
            if (useDependencyInjection)
            {
                services.UseDependencyInjection();
            }
        }
    }
}
#endif
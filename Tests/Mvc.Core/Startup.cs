using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkyBuilding;
using SkyBuilding.Cache;
using SkyBuilding.Config;
using SkyBuilding.Log;
using SkyBuilding.Mvc;
using SkyBuilding.Mvc.Cache;
using SkyBuilding.Mvc.Config;
using SkyBuilding.Mvc.Converters;
using SkyBuilding.Mvc.Log;
using SkyBuilding.Mvc.Serialize.Json;
using SkyBuilding.Serialize.Json;
using Swashbuckle.AspNetCore.Swagger;

namespace Mvc.Core
{
    //public class Startup
    //{
    //    public Startup(IConfiguration configuration)
    //    {
    //        Configuration = configuration;
    //    }

    //    public IConfiguration Configuration { get; }

    //    // This method gets called by the runtime. Use this method to add services to the container.
    //    public void ConfigureServices(IServiceCollection services)
    //    {
    //        services.AddControllers();
    //    }

    //    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    //    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    //    {
    //        if (env.IsDevelopment())
    //        {
    //            app.UseDeveloperExceptionPage();
    //        }

    //        app.UseRouting();

    //        app.UseAuthorization();

    //        app.UseEndpoints(endpoints =>
    //        {
    //            endpoints.MapControllers();
    //        });
    //    }
    //}

    /// <inheritdoc />
    public class Startup
    {
        /// <inheritdoc />
        public void ConfigureServices(IServiceCollection services)
        {
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
            //? MVC
            services
                .AddMvc(options =>
                {
                    //自定义异常捕获
                    options.Filters.Add<DExceptionFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new SkyJsonConverter());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();
            RuntimeServManager.TryAddSingleton<IConfigHelper, DefaultConfigHelper>();

            //? 日志服务
            LogManager.AddAdapter(new Log4NetAdapter());

            //? 缓存服务
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.First);
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.Second);

            //增加XML文档解析
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "API接口", Version = "v3" });
                var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    c.IncludeXmlComments(file);
                }
            });

            services.AddAutofac(container => IocRegisters(container));
        }
        /// <inheritdoc />
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

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

            var assemblys =
                Directory.GetFiles(path, "*.dll")
                    .Select(Assembly.LoadFrom)
                    .ToArray();

            builder.RegisterAssemblyTypes(assemblys)
                .Where(type => !type.IsAbstract)
                .AsSelf() //自身服务，用于没有接口的类
                .AsImplementedInterfaces() //接口服务
                .PropertiesAutowired()//属性注入
                .InstancePerLifetimeScope(); //保证生命周期基于请求

            builder.RegisterAssemblyTypes(assemblys)
                .Where(type => !type.IsAbstract)
                .AsSelf() //自身服务，用于没有接口的类
                .AsImplementedInterfaces() //接口服务
                .PropertiesAutowired(); //属性注入

            builder.RegisterAssemblyTypes(assemblys)
                .Where(type => !type.IsAbstract)
                .AsSelf() //自身服务，用于没有接口的类
                .AsImplementedInterfaces() //接口服务
                .PropertiesAutowired()//属性注入
                .SingleInstance(); //保证单例注入

            return builder.Build();
        }
    }
}

#if NETSTANDARD2_0 || NETCOREAPP3_0
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SkyBuilding.Cache;
using SkyBuilding.Config;
using SkyBuilding.Log;
using SkyBuilding.Serialize.Json;
using SkyBuilding.Mvc.Converters;
using System;
using System.IO;
using System.Linq;
#if NETCOREAPP3_0
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Hosting;
#else
using Swashbuckle.AspNetCore.Swagger;
#endif

namespace SkyBuilding.Mvc
{
    /// <summary>
    /// 启动基类
    /// </summary>
    public class DStartup
    {
        /// <summary>
        /// 配置服务（这个方法被运行时调用。使用此方法向容器添加服务。）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns></returns>

        public virtual void ConfigureServices(IServiceCollection services)
        {
#if NETCOREAPP3_0
            services.AddControllers();

            services
                .AddMvc(options =>
                {
                    options.EnableEndpointRouting = false;
                    //自定义异常捕获
                    options.Filters.Add<DExceptionFilter>();
                }).AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new SkyJsonConverter());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
#else
            services
                .AddMvc(options =>
                {
                    //自定义异常捕获
                    options.Filters.Add<DExceptionFilter>();
                }).AddJsonOptions(options =>
                {
                    options.SerializerSettings.DateFormatString = Consts.DateFormatString;
                    options.SerializerSettings.Converters.Add(new SkyJsonConverter());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
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

            //? 日志服务
            LogManager.AddAdapter(new Log4NetAdapter());

            //? 缓存服务
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.First);
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.Second);

            //增加XML文档解析
            services.AddSwaggerGen(c =>
            {
#if NETCOREAPP3_0
                c.SwaggerDoc("swagger:version".Config(Consts.SwaggerVersion), new OpenApiInfo { Title = "swagger:title".Config(Consts.SwaggerTitle), Version = "v3" });
#else
                c.SwaggerDoc("swagger:version".Config(Consts.SwaggerVersion), new Info { Title = "swagger:title".Config(Consts.SwaggerTitle), Version = "v3" });
#endif
                var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    c.IncludeXmlComments(file);
                }
            });
        }

#if NETCOREAPP3_0
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

#if NETCOREAPP3_0
            //? 跨域
            app.UseStaticFiles()
                .UseRouting()
                .UseMvc()
                .UseCors("Allow")
                .UseSwagger()
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/" + "swagger:version".Config(Consts.SwaggerVersion) + "/swagger.json", "swagger:title".Config(Consts.SwaggerTitle));
                })
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
#else
            //? 跨域
            app.UseStaticFiles()
                .UseCors("Allow")
                .UseMvc()
                .UseSwagger()
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/" + "swagger:version".Config(Consts.SwaggerVersion) + "/swagger.json", "swagger:title".Config(Consts.SwaggerTitle));
                });
#endif
        }
    }
}
#endif
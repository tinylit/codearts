#if NETSTANDARD2_0 || NETCOREAPP3_0
using Autofac;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using SkyBuilding.Cache;
using SkyBuilding.Config;
using SkyBuilding.Exceptions;
using SkyBuilding.Log;
using SkyBuilding.Serialize.Json;
using SkyBuilding.Mvc.Converters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
#if NETCOREAPP3_0
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Hosting;
#else
using Swashbuckle.AspNetCore.Swagger;
using Autofac.Extensions.DependencyInjection;
#endif

namespace SkyBuilding.Mvc
{
    /// <summary>
    /// 启动基类
    /// </summary>
    public class DStartup
    {
#if NETCOREAPP3_0
        /// <summary>
        /// 配置服务
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
#else
        /// <summary>
        /// 配置服务
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public virtual IServiceProvider ConfigureServices(IServiceCollection services)
        {
#endif
#if NETCOREAPP3_0
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
                    options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK";
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
                c.SwaggerDoc("swagger:version".Config("v1"), new OpenApiInfo { Title = "swagger:title".Config("API接口文档"), Version = "v3" });
#else
                c.SwaggerDoc("swagger:version".Config("v1"), new Info { Title = "swagger:title".Config("API接口文档"), Version = "v3" });
#endif
                var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    c.IncludeXmlComments(file);
                }
            });
#if NETSTANDARD2_0
            var container = new ContainerBuilder();

            container.Populate(services);

            ConfigureContainer(container);

            return new AutofacServiceProvider(container.Build());
#endif
        }

        /// <summary>
        /// 配置容器
        /// </summary>
        /// <param name="builder">容器</param>
        public virtual void ConfigureContainer(ContainerBuilder builder)
        {
            var path = AppDomain.CurrentDomain.RelativeSearchPath;
            if (!Directory.Exists(path))
                path = AppDomain.CurrentDomain.BaseDirectory;

            var assemblys = Directory.GetFiles(path, "*.dll")
                    .Select(Assembly.LoadFrom);

            var assemblyTypes = assemblys.SelectMany(x => x.GetTypes());

            var controllerTypes = assemblyTypes
                .Where(type => type.IsClass && !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type));

            var interfaceTypes = controllerTypes
                .SelectMany(type =>
                {
                    return type.GetConstructors()
                          .SelectMany(x => x.GetParameters()
                          .Select(y => y.ParameterType));
                })
                .Distinct();

            if (assemblys.Any(x => x.FullName.StartsWith("SkyBuilding.ORM")))
            {
                var repositoryTypes = assemblys.Where(x => x.FullName.StartsWith("SkyBuilding.ORM"))
                    .SelectMany(x => x.GetTypes().Where(y => y.IsClass && y.IsAbstract && y.FullName == "SkyBuilding.ORM.Repository"));

                builder.RegisterTypes(assemblyTypes.Where(type => type.IsClass && repositoryTypes.Any(x => x.IsAssignableFrom(type))).ToArray())
                    .AsSelf()
                    .AsImplementedInterfaces()
                    .PropertiesAutowired()
                    .SingleInstance();
            }

            var types = interfaceTypes.SelectMany(x => assemblyTypes.Where(y => x.IsAssignableFrom(y)));

            builder.RegisterTypes(types.Union(interfaceTypes).Union(controllerTypes).ToArray())
                .Where(type => type.IsInterface || type.IsClass)
                .AsSelf() //自身服务，用于没有接口的类
                .AsImplementedInterfaces() //接口服务
                .PropertiesAutowired(); //属性注入
        }

#if NETCOREAPP3_0
        /// <summary>
        /// 配置管道
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
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
                    c.SwaggerEndpoint("/swagger/" + "swagger:version".Config("v1") + "/swagger.json", "swagger:title".Config("API接口文档"));
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
                    c.SwaggerEndpoint("/swagger/" + "swagger:version".Config("v1") + "/swagger.json", "swagger:title".Config("API接口文档"));
                });
#endif
        }
    }

    /// <summary>
    /// Jwt 认证基类
    /// </summary>
    public class JwtStartup : DStartup
    {
        private readonly char[] CharArray = "0123456789ABCDEabcdefghigklmnopqrFGHIGKLMNOPQRSTUVWXYZstuvwxyz".ToCharArray();

        /// <summary>
        /// 验证码
        /// </summary>
        protected ICache AuthCode => CacheManager.GetCache("auth-code", CacheLevel.Second);

#if NETCOREAPP3_0
        public override void ConfigureServices(IServiceCollection services)
#else
        public override IServiceProvider ConfigureServices(IServiceCollection services)
#endif
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Authority = "jwt:authority".Config<string>();
                options.Audience = "jwt:audience".Config("api");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                    LifetimeValidator = (before, expires, token, param) =>
                    {
                        return token.ValidTo > DateTime.UtcNow;
                    },
                    // 用于适配本地模拟Token
                    ValidIssuer = "jwt:issuer".Config("yep"),
                    ValidateIssuer = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("jwt:secret".Config(Consts.Secret)))
                };
            });
#if NETCOREAPP3_0
            base.ConfigureServices(services);
#else
            return base.ConfigureServices(services);
#endif
        }
#if NETCOREAPP3_0
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
#else
        public override void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
#endif
            app.UseAuthentication();

            base.Configure(app, env);

            app.Use(next =>
                {
                    return async context =>
                    {
                        try
                        {
                            await next.Invoke(context);
                        }
                        catch (Exception exception)
                        {
                            await context.Response.WriteJsonAsync(ExceptionHandler.Handler(exception));
                        }
                    };
                });

            //? 验证码路由
            app.Map("/authCode", builder => builder.Run(async context =>
            {
                string code = CreateRandomCode(4); //验证码的字符为4个
                byte[] bytes = CreateValidateGraphic(code);

                string id = context.GetRemoteMacAddress() ?? context.GetRemoteIpAddress();
                string url = context.GetRefererUrlStrings();

                string md5 = $"{id}-{url}".Md5();

                AuthCode.Set(md5, code, TimeSpan.FromMinutes(2D));

                await context.Response.WriteImageAsync(bytes);
            }));

            app.Map("/login", builder => builder.Run(async context =>
            {
                if (!env.IsDevelopment() || !context.Request.Query.TryGetValue("debug", out StringValues debug) || !bool.TryParse(debug, out bool isDebug) || !isDebug)
                {
                    if (!context.Request.Query.TryGetValue("authCode", out StringValues value) || value == StringValues.Empty)
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("验证码不能为空!"));
                        return;
                    }

                    string id = context.GetRemoteMacAddress() ?? context.GetRemoteIpAddress();
                    string url = context.GetRefererUrlStrings();

                    string md5 = $"{id}-{url}".Md5();

                    string authCache = AuthCode.Get<string>(md5);

                    if (string.IsNullOrEmpty(authCache))
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("验证码已过期!"));
                        return;
                    }

                    string authCode = value.ToString();

                    if (authCode.Trim().ToLower() != authCache.Trim().ToLower())
                    {
                        await context.Response.WriteJsonAsync(DResult.Error("验证码错误!"));
                        return;
                    }
                }

                var loginUrl = "login".Config<string>();

                if (string.IsNullOrEmpty(loginUrl))
                {
                    await context.Response.WriteJsonAsync(DResult.Error("未配置登录接口!", StatusCodes.ServError));
                    return;
                }

                if (loginUrl.IsUrl() ? !Uri.TryCreate(loginUrl, UriKind.Absolute, out Uri loginUri) : !Uri.TryCreate($"{context.Request.Scheme}://{context.Request.Host}/{loginUrl.TrimStart('/')}", UriKind.Absolute, out loginUri))
                {
                    await context.Response.WriteJsonAsync(DResult.Error("不规范的登录接口!", StatusCodes.NonstandardServerError));
                    return;
                }

                var result = loginUri.AsRequestable()
                .Query(context.Request.QueryString.Value)
                .ByJson<ServResult<Dictionary<string, object>>>()
                .Get();

                if (result.Success)
                {
                    await WriteToken(context, result.Data);
                }
                else
                {
                    await context.Response.WriteJsonAsync(result);
                }

            }));
        }

        #region Private
        /// <summary>
        /// 生成随机的字符串
        /// </summary>
        /// <param name="codeCount"></param>
        /// <returns></returns>
        private string CreateRandomCode(int codeCount)
        {
            Random rand = new Random();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < codeCount; i++)
            {
                sb.Append(CharArray[rand.Next(35)]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 创建验证码图片
        /// </summary>
        /// <param name="validateCode"></param>
        /// <returns></returns>
        private byte[] CreateValidateGraphic(string validateCode)
        {
            Bitmap image = new Bitmap((int)Math.Ceiling(validateCode.Length * 16.0), 27);
            Graphics g = Graphics.FromImage(image);
            try
            {
                //生成随机生成器
                Random random = new Random();
                //清空图片背景色
                g.Clear(Color.White);
                //画图片的干扰线
                for (int i = 0; i < 25; i++)
                {
                    int x1 = random.Next(image.Width);
                    int x2 = random.Next(image.Width);
                    int y1 = random.Next(image.Height);
                    int y2 = random.Next(image.Height);
                    g.DrawLine(new Pen(Color.Silver), x1, x2, y1, y2);
                }
                Font font = new Font("Arial", 13, (FontStyle.Bold | FontStyle.Italic));
                LinearGradientBrush brush = new LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height), Color.Blue, Color.DarkRed, 1.2f, true);
                g.DrawString(validateCode, font, brush, 3, 2);

                //画图片的前景干扰线
                for (int i = 0; i < 100; i++)
                {
                    int x = random.Next(image.Width);
                    int y = random.Next(image.Height);
                    image.SetPixel(x, y, Color.FromArgb(random.Next()));
                }
                //画图片的边框线
                g.DrawRectangle(new Pen(Color.Silver), 0, 0, image.Width - 1, image.Height - 1);

                //保存图片数据
                MemoryStream stream = new MemoryStream();
                image.Save(stream, ImageFormat.Png);

                //输出图片流
                return stream.ToArray();
            }
            finally
            {
                g.Dispose();
                image.Dispose();
            }
        }

        /// <summary>
        /// 写入Token
        /// </summary>
        /// <param name="user">用户</param>
        /// <returns></returns>
        private async Task WriteToken(HttpContext context, Dictionary<string, object> user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes("jwt:secret".Config(Consts.Secret));

            var expires = DateTime.UtcNow.AddDays(1D);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = user.AsIdentity(),
                Expires = expires,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

            await context.Response.WriteJsonAsync(DResult.Ok(new
            {
                token,
                type = "Bearer"
            }));
        }
        #endregion
    }
}
#endif
#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
#if NETCOREAPP3_1
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Models;
#else
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Swagger;
#endif
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Security.Claims;
using System.Text;

namespace CodeArts.Mvc
{
    /// <summary>
    /// Jwt 认证基类
    /// </summary>
    public class JwtStartup : DStartup
    {
        private readonly bool useJwtAuth;
        private readonly PathString basePath = new PathString("/");

#if NETCOREAPP3_1
        /// <summary>
        /// 构造函数【默认验证码、注册、登录的根路径为“/”】。
        /// </summary>
        /// <param name="useSwaggerUi">使用SwaggerUi</param>
        /// <param name="useDependencyInjection">使用依赖注入：<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        /// <param name="useJwtAuth">使用Jwt认证登录、注册、验证码功能：<see cref="AuthApplicationBuilderExtentions.UseJwtAuth(IEndpointRouteBuilder, PathString)"/></param>
#else
        /// <summary>
        /// 构造函数【默认验证码、注册、登录的根路径为“/”】。
        /// </summary>
        /// <param name="useSwaggerUi">使用SwaggerUi</param>
        /// <param name="useDependencyInjection">使用依赖注入：<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        /// <param name="useJwtAuth">使用Jwt认证登录、注册、验证码功能：<see cref="AuthApplicationBuilderExtentions.UseJwtAuth(IApplicationBuilder, PathString)"/></param>
#endif
        public JwtStartup(bool useSwaggerUi = true, bool useDependencyInjection = true, bool useJwtAuth = true) : base(useSwaggerUi, useDependencyInjection)
        {
            this.useJwtAuth = useJwtAuth;
        }
#if NETCOREAPP3_1 
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="basePath">用于验证码、注册、登录的根路径。</param>
        /// <param name="useSwaggerUi">使用SwaggerUi</param>
        /// <param name="useDependencyInjection">使用依赖注入：<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        /// <param name="useJwtAuth">使用Jwt认证登录、注册、验证码功能：<see cref="AuthApplicationBuilderExtentions.UseJwtAuth(IEndpointRouteBuilder, PathString)"/></param>
#else
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="basePath">用于验证码、注册、登录的根路径。</param>
        /// <param name="useSwaggerUi">使用SwaggerUi</param>
        /// <param name="useDependencyInjection">使用依赖注入：<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        /// <param name="useJwtAuth">使用Jwt认证登录、注册、验证码功能：<see cref="AuthApplicationBuilderExtentions.UseJwtAuth(IApplicationBuilder, PathString)"/></param>
#endif
        public JwtStartup(PathString basePath, bool useSwaggerUi = true, bool useDependencyInjection = true, bool useJwtAuth = true) : base(useSwaggerUi, useDependencyInjection)
        {
            this.basePath = basePath;
            this.useJwtAuth = useJwtAuth;
        }

        /// <summary>
        /// 服务配置（这个方法被运行时调用。使用此方法向容器添加服务。）
        /// 通过“jwt:authority”配置 <see cref="JwtBearerOptions.Authority"/>。
        /// </summary>
        /// <param name="services">服务集合</param>
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                options.Authority = "jwt:authority".Config<string>();
                options.Audience = "jwt:audience".Config(Consts.JwtAudience);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                    LifetimeValidator = (before, expires, token, param) =>
                    {
                        return token.ValidTo > DateTime.UtcNow;
                    },
                    // 用于适配本地模拟Token
                    ValidIssuer = "jwt:issuer".Config(Consts.JwtIssuer),
                    ValidateIssuer = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("jwt:secret".Config(Consts.JwtSecret)))
                };
            });

            base.ConfigureServices(services);
        }

        /// <summary>
        /// 配置SwaggerGen。
        /// </summary>
        /// <param name="options">SwaggerGen配置项</param>
        protected override void ConfigureSwaggerGen(SwaggerGenOptions options)
        {
            base.ConfigureSwaggerGen(options);

#if NETCOREAPP3_1
            var scheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            };

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [scheme] = Array.Empty<string>()
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "请输入【Bearer {token}】，注意中间有一个空格!",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });
#else
            options.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
            {
                ["Bearer"] = Array.Empty<string>()
            });

            options.AddSecurityDefinition("Bearer", new ApiKeyScheme
            {
                Name = "Authorization",
                Description = "请输入【Bearer {token}】，注意中间有一个空格!",
                In = "header"
            });
#endif
        }

        /// <summary>
        /// 配置管道（此方法由运行时调用。使用此方法配置HTTP请求管道。）
        /// </summary>
        /// <param name="app">项目构建器</param>
        /// <param name="env">环境变量</param>
#if NETCOREAPP3_1
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
#else
        public override void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (useJwtAuth)
            {
                app.UseJwtAuth(basePath);
            }

#endif

            app.UseAuthentication();

            base.Configure(app, env);
        }

#if NETCOREAPP3_1
        /// <summary>
        /// 使用端点。
        /// </summary>
        /// <param name="endpoints">端点路由构造器</param>
        protected override void UseEndpoints(IEndpointRouteBuilder endpoints)
        {
            if (useJwtAuth)
            {
                endpoints.UseJwtAuth(basePath);
            }

            base.UseEndpoints(endpoints);
        }
#endif
    }
}
#else
using CodeArts.Mvc.Authentication;
using CodeArts.Mvc.Builder;
using CodeArts.Mvc.DependencyInjection;
#if !NET40
using Swashbuckle.Application;
#endif

namespace CodeArts.Mvc
{
    /// <summary>
    /// 启动基类。
    /// </summary>
    public class JwtStartup : DStartup
    {
        private readonly bool useJwtAuth;
        private readonly PathString basePath = new PathString("/");

#if NET40
        /// <summary>
        /// 构造函数【默认验证码、注册、登录的根路径为“/”】。
        /// </summary>
        /// <param name="useDependencyInjection">使用依赖注入：<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        /// <param name="useJwtAuth">使用Jwt认证登录、注册、验证码功能：<see cref="AuthApplicationBuilderExtentions.UseJwtAuth(IApplicationBuilder, PathString)"/></param>
        public JwtStartup(bool useDependencyInjection = true, bool useJwtAuth = true) : base(useDependencyInjection)
        {
            this.useJwtAuth = useJwtAuth;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="basePath">用于验证码、注册、登录的根路径。</param>
        /// <param name="useDependencyInjection">使用依赖注入：<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        /// <param name="useJwtAuth">使用Jwt认证登录、注册、验证码功能：<see cref="AuthApplicationBuilderExtentions.UseJwtAuth(IApplicationBuilder, PathString)"/></param>
        public JwtStartup(PathString basePath, bool useDependencyInjection = true, bool useJwtAuth = true) : base(useDependencyInjection)
        {
            this.basePath = basePath;
            this.useJwtAuth = useJwtAuth;
        }
#else

        /// <summary>
        /// 构造函数【默认验证码、注册、登录的根路径为“/”】。
        /// </summary>
        /// <param name="useSwaggerUi">使用SwaggerUi</param>
        /// <param name="useDependencyInjection">使用依赖注入：<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        /// <param name="useJwtAuth">使用Jwt认证登录、注册、验证码功能：<see cref="AuthApplicationBuilderExtentions.UseJwtAuth(IApplicationBuilder, PathString)"/></param>
        public JwtStartup(bool useSwaggerUi = true, bool useDependencyInjection = true, bool useJwtAuth = true) : base(useSwaggerUi, useDependencyInjection)
        {
            this.useJwtAuth = useJwtAuth;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="basePath">用于验证码、注册、登录的根路径。</param>
        /// <param name="useSwaggerUi">使用SwaggerUi</param>
        /// <param name="useDependencyInjection">使用依赖注入：<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        /// <param name="useJwtAuth">使用Jwt认证登录、注册、验证码功能：<see cref="AuthApplicationBuilderExtentions.UseJwtAuth(IApplicationBuilder, PathString)"/></param>
        public JwtStartup(PathString basePath, bool useSwaggerUi = true, bool useDependencyInjection = true, bool useJwtAuth = true) : base(useSwaggerUi, useDependencyInjection)
        {
            this.basePath = basePath;
            this.useJwtAuth = useJwtAuth;
        }

        /// <summary>
        /// 配置Swagger。
        /// </summary>
        /// <param name="config">Swagger文档配置项</param>
        protected override void ConfigureSwagger(SwaggerDocsConfig config)
        {
            config.ApiKey("Authorization")
                .Description("API Key Authentication")
                .In("header")
                .Name("Authorization");

            base.ConfigureSwagger(config);
        }

        /// <summary>
        /// 配置SwaggerUi。
        /// </summary>
        /// <param name="config">SwaggerUi配置项</param>
        protected override void ConfigureSwaggerUi(SwaggerUiConfig config)
        {
            config.EnableApiKeySupport("Authorization", "header");

            base.ConfigureSwaggerUi(config);
        }
#endif

        /// <summary>
        /// 配置中间件。
        /// </summary>
        /// <param name="builder">方案构造器</param>
        public virtual void Configure(IApplicationBuilder builder)
        {
            if (useJwtAuth)
            {
                builder.UseJwtAuth(basePath);
            }

            builder.UseJwtBearer(JwtBearerEvents.Authorization);
        }
    }
}
#endif
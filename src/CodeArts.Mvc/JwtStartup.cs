﻿#if NETCOREAPP2_0_OR_GREATER
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
#if NETCOREAPP3_1
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
    /// Jwt 认证基类。
    /// </summary>
    public class JwtStartup : DStartup
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public JwtStartup()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="useSwaggerUi">使用SwaggerUi。</param>
        public JwtStartup(bool useSwaggerUi) : base(useSwaggerUi)
        {
        }

        /// <summary>
        /// 服务配置（这个方法被运行时调用。使用此方法向容器添加服务）。
        /// 通过“jwt:authority”配置 <see cref="JwtBearerOptions.Authority"/>。
        /// </summary>
        /// <param name="services">服务集合。</param>
        public override void ConfigureServices(IServiceCollection services)
        {
            ConfigureAuthentication(services);

            base.ConfigureServices(services);
        }

        /// <summary>
        /// 配置认证（配置JWT认证）。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns></returns>
        protected virtual AuthenticationBuilder ConfigureAuthentication(IServiceCollection services)
        => services.AddAuthentication(options =>
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

        /// <summary>
        /// 配置SwaggerGen。
        /// </summary>
        /// <param name="options">SwaggerGen配置项。</param>
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
                Description = "swagger:description".Config(Consts.SwaggerDescription),
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
                Description = "swagger:description".Config(Consts.SwaggerDescription),
                In = "header"
            });
#endif
        }

        /// <summary>
        /// 配置管道（此方法由运行时调用。使用此方法配置HTTP请求管道）。
        /// </summary>
        /// <param name="app">项目构建器。</param>
        /// <param name="env">环境变量。</param>
#if NETCOREAPP3_1
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
#else
        public override void Configure(IApplicationBuilder app, IHostingEnvironment env)
#endif
        {
            app.UseAuthentication();

            base.Configure(app, env);
        }
    }
}
#else
using CodeArts.Mvc.Authentication;
using CodeArts.Mvc.Builder;
#if NET45_OR_GREATER
using Swashbuckle.Application;
using System;
#endif

namespace CodeArts.Mvc
{
    /// <summary>
    /// 启动基类。
    /// </summary>
    public class JwtStartup : DStartup
    {
#if NET45_OR_GREATER
        /// <summary>
        /// 构造函数。
        /// </summary>
        public JwtStartup()
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="useSwaggerUi">使用SwaggerUi。</param>
        public JwtStartup(bool useSwaggerUi) : base(useSwaggerUi)
        {
        }

        /// <summary>
        /// 配置Swagger。
        /// </summary>
        /// <param name="config">Swagger文档配置项。</param>
        protected override void ConfigureSwagger(SwaggerDocsConfig config)
        {
            config.ApiKey("Authorization")
                .Description("swagger-description".Config(Consts.SwaggerDescription))
                .In("header")
                .Name("Authorization");

            base.ConfigureSwagger(config);
        }

        /// <summary>
        /// 配置SwaggerUi。
        /// </summary>
        /// <param name="config">SwaggerUi配置项。</param>
        protected override void ConfigureSwaggerUi(SwaggerUiConfig config)
        {
            config.EnableApiKeySupport("Authorization", "header");

            base.ConfigureSwaggerUi(config);
        }
#endif

        /// <summary>
        /// 配置中间件。
        /// </summary>
        /// <param name="builder">方案构造器。</param>
        public virtual void Configure(IApplicationBuilder builder) => builder.UseJwtBearer(JwtBearerEvents.Authorization);
    }
}
#endif
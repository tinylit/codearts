#if NETSTANDARD2_0 || NETCOREAPP3_1
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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
        private readonly PathString basePath = new PathString("/");

        /// <summary>
        /// 构造函数【默认验证码、注册、登录的根路径为“/”】。
        /// </summary>
        /// <param name="useSwaggerUi">使用SwaggerUi</param>
        /// <param name="useDependencyInjection">使用依赖注入<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        public JwtStartup(bool useSwaggerUi = true, bool useDependencyInjection = true) : base(useSwaggerUi, useDependencyInjection)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="basePath">用于验证码、注册、登录的根路径。</param>
        /// <param name="useSwaggerUi">使用SwaggerUi</param>
        /// <param name="useDependencyInjection">使用依赖注入<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        public JwtStartup(PathString basePath, bool useSwaggerUi = true, bool useDependencyInjection = true) : base(useSwaggerUi, useDependencyInjection)
        {
            this.basePath = basePath;
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
#endif
            app.UseJwtAuth(basePath);

            app.UseAuthentication();

            base.Configure(app, env);
        }
    }
}
#else
using CodeArts.Mvc.Authentication;
using CodeArts.Mvc.Builder;
using CodeArts.Mvc.DependencyInjection;

namespace CodeArts.Mvc
{
    /// <summary>
    /// 启动基类。
    /// </summary>
    public class JwtStartup : DStartup
    {
        private readonly PathString basePath = new PathString("/");

        /// <summary>
        /// 构造函数【默认验证码、注册、登录的根路径为“/”】。
        /// </summary>
        /// <param name="useSwaggerUi">使用SwaggerUi</param>
        /// <param name="useDependencyInjection">使用依赖注入<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        public JwtStartup(bool useSwaggerUi = true, bool useDependencyInjection = true) : base(useSwaggerUi, useDependencyInjection)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="basePath">用于验证码、注册、登录的根路径。</param>
        /// <param name="useSwaggerUi">使用SwaggerUi</param>
        /// <param name="useDependencyInjection">使用依赖注入<see cref="DependencyInjectionServiceCollectionExtentions.UseDependencyInjection(IServiceCollection)"/></param>
        public JwtStartup(PathString basePath, bool useSwaggerUi = true, bool useDependencyInjection = true) : base(useSwaggerUi, useDependencyInjection)
        {
            this.basePath = basePath;
        }

        /// <summary>
        /// 配置中间件。
        /// </summary>
        /// <param name="builder">方案构造器</param>
        public virtual void Configure(IApplicationBuilder builder) => builder.UseJwtAuth(basePath).UseJwtBearer(JwtBearerEvents.Authorization);
    }
}
#endif
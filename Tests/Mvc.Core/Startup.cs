using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using CodeArts.Mvc;
using CodeArts.MySql;
using CodeArts.ORM;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using CodeArts.Serialize.Json;

namespace Mvc.Core
{
    /// <inheritdoc />
    public class Startup : JwtStartup
    {
        /// <inheritdoc />
        public Startup() : base(new PathString("/api"))
        {
        }
        /// <inheritdoc />
        public override void ConfigureServices(IServiceCollection services)
        {
            DbConnectionManager.RegisterAdapter(new MySqlAdapter());
            DbConnectionManager.RegisterProvider<CodeArtsProvider>();

            //services.AddGrpc();

            var x = JsonHelper.ToJson(new { x = 1 });

            ModelValidator.CustomValidate<RequiredAttribute>((attr, context) =>
            {
                return $"{context.DisplayName}Îª±ØÌî×Ö¶Î!";
            });

            base.ConfigureServices(services);
        }

        /// <inheritdoc />
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            base.Configure(app.MapPost("/test", "/api/values/test"), env);

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapGrpcService<PushService>();

            //    endpoints.MapGet("/", async context =>
            //    {
            //        await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            //    });
            //});
        }
    }
}

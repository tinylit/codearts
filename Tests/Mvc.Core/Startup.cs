using CodeArts;
using CodeArts.Db.EntityFramework;
using CodeArts.Db.Lts;
using CodeArts.Mvc;
using CodeArts.Serialize.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Mvc.Core.Domain;
using System.ComponentModel.DataAnnotations;

namespace Mvc.Core
{
    /// <inheritdoc />
    public class Startup : JwtStartup
    {
        /// <inheritdoc />
        public Startup()
        {
            using (var startup = new XStartup())
            {
                startup.DoStartup();
            }
        }
        /// <inheritdoc />
        public override void ConfigureServices(IServiceCollection services)
        {
            DbConnectionManager.RegisterAdapter(new MySqlLtsAdapter());
            DbConnectionManager.RegisterProvider<CodeArtsProvider>();

            LinqConnectionManager.RegisterAdapter(new SqlServerLinqAdapter());

            services.AddDefaultRepositories<EfContext>();

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

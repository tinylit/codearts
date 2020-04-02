using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using CodeArts.Mvc;
using CodeArts.MySql;
using CodeArts.ORM;

namespace Mvc.Core
{
    public class Startup : JwtStartup
    {
        public Startup() : base(new PathString("/api"))
        {
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            DbConnectionManager.AddAdapter(new MySqlAdapter());
            DbConnectionManager.AddProvider<CodeArtsProvider>();

            services.AddGrpc();

            base.ConfigureServices(services);
        }

        /// <inheritdoc />
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            base.Configure(app.MapPost("/test", "/api/values/test"), env);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<PushService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}

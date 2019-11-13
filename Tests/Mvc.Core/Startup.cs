using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SkyBuilding.Mvc;
using SkyBuilding.MySql;
using SkyBuilding.ORM;

namespace Mvc.Core
{
    public class Startup : JwtStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            DbConnectionManager.AddAdapter(new MySqlAdapter());
            DbConnectionManager.AddProvider<SkyProvider>();

            services.AddGrpc();

            //? “¿¿µ◊¢»Î
            services.UseDependencyInjection();

            base.ConfigureServices(services);
        }
        /// <inheritdoc />
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            base.Configure(app, env);

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

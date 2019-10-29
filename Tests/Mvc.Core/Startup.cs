using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkyBuilding;
using SkyBuilding.Cache;
using SkyBuilding.Config;
using SkyBuilding.Log;
using SkyBuilding.Mvc;
using SkyBuilding.Mvc.Converters;
using SkyBuilding.MySql;
using SkyBuilding.ORM;
using SkyBuilding.Serialize.Json;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Mvc.Core
{
    public class Startup : JwtStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            DbConnectionManager.AddAdapter(new MySqlAdapter());
            DbConnectionManager.AddProvider<SkyProvider>();

            services.AddGrpc();

            base.ConfigureServices(services);
        }

        /// <inheritdoc />
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            base.Configure(app, env);

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapGrpcService<PushService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}

using CodeArts;
using CodeArts.Mvc;
using CodeArts.Mvc.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Mvc4
{
    public class Startup : JwtStartup
    {
        public Startup()
        {
            using (var startup = new XStartup())
            {
                startup.DoStartup();
            }
        }
        public void Configure(IApplicationBuilder builder, IServiceProvider serviceProvider)
        {
            base.Configure(builder.MapPost("/test", "/api/values/test"));
        }

        /// <inheritdoc />
        public void ConfigureServices(IServiceCollection services)
        {
            services.UseDependencyInjection();
        }
    }
}
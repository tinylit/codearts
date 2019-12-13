using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkyBuilding.Mvc;

namespace Mvc.Core2_2
{
    /// <inheritdoc />
    public class Startup : JwtStartup
    {
        public override void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();

            loggerFactory.AddLog4Net();

            base.Configure(app, env);
        }
    }
}

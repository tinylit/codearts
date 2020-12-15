using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CodeArts.Mvc;
using CodeArts.Db;
using CodeArts;
using CodeArts.Db.Lts;

namespace Mvc.Core2_1
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

            services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
            services.AddTransient(typeof(IDbRepository<>), typeof(DbRepository<>));

            base.ConfigureServices(services);
        }

        /// <inheritdoc />
        public override void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();

            loggerFactory.AddLog4Net();

            base.Configure(app, env);
        }
    }
}

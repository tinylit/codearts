using CodeArts;
using CodeArts.Db.EntityFramework;
using CodeArts.Db.Lts;
using CodeArts.Mvc;
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
            LinqConnectionManager.RegisterAdapter(new SqlServerLinqAdapter());
            DbConnectionManager.RegisterDatabaseFor<DapperFor>();

            services.AddSingleton<IDbTransactionProvider, DbTransactionProvider>()
                .AddDefaultRepositories<EfContext>();

            ModelValidator.CustomValidate<RequiredAttribute>((attr, context) =>
            {
                return $"{context.DisplayName}Îª±ØÌî×Ö¶Î!";
            });

            base.ConfigureServices(services);

            services
                .UseDependencyInjection()
                .UseMiddleware();
        }
    }
}

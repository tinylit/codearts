using CodeArts;
using CodeArts.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Mvc45
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

        /// <inheritdoc />
        public void ConfigureServices(IServiceCollection services)
        {
            services.UseDependencyInjection();
        }
    }
}
using SkyBuilding.Mvc;
using SkyBuilding.Mvc.DependencyInjection;

namespace Mvc45
{
    public class Startup : JwtStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.UseDependencyInjection();
        }
    }
}
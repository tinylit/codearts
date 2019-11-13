using SkyBuilding.Mvc;
using System.Web;
using System.Web.Http;

namespace Mvc4
{
    /// <inheritdoc />
    public class WebApiApplication : HttpApplication
    {
        /// <inheritdoc />
        protected void Application_Start()
        {
            ApiConfig.Register(GlobalConfiguration.Configuration);

            ApiConfig.UseDependencyInjection(GlobalConfiguration.Configuration);
        }
    }
}

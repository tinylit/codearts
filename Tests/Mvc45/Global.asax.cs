using SkyBuilding.Mvc;
using System.Web;
using System.Web.Http;

namespace Mvc45
{
    /// <inheritdoc />
    public class WebApiApplication : HttpApplication
    {
        /// <inheritdoc />
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(ApiConfig.Register);

            GlobalConfiguration.Configure(ApiConfig.SwaggerUI);

            GlobalConfiguration.Configure(ApiConfig.UseDependencyInjection);
        }
    }
}

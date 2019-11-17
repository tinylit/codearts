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
            GlobalConfiguration.Configuration.Register()
                                             .UseDependencyInjection();
        }
    }
}

using SkyBuilding.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace MVC461
{
    /// <inheritdoc />
    public class WebApiApplication : HttpApplication
    {
        /// <inheritdoc />
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(ApiConfig.Register);
        }
    }
}

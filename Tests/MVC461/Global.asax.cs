using SkyBuilding.Mvc;
using SkyBuilding.MySql;
using SkyBuilding.ORM;
using System;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Http;

namespace Mvc461
{
    /// <inheritdoc />
    public class WebApiApplication : HttpApplication
    {
        /// <inheritdoc />
        protected void Application_Start()
        {
            DbConnectionManager.AddAdapter(new MySqlAdapter());
            DbConnectionManager.AddProvider<SkyProvider>();

            DbValidator.CustomValidate<RequiredAttribute>((attr, context) =>
            {
                if (attr.AllowEmptyStrings)
                {
                    return "{DisplayName}不能为Null!".PropSugar(context);
                }

                return "{DisplayName}不能为空!".PropSugar(context);
            });

            GlobalConfiguration.Configure(ApiConfig.Register);

            GlobalConfiguration.Configure(ApiConfig.SwaggerUI);

            GlobalConfiguration.Configure(ApiConfig.UseDependencyInjection);
        }
    }
}

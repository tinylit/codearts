using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using SkyBuilding;
using SkyBuilding.Cache;
using SkyBuilding.Mvc;
using SkyBuilding.Mvc.Builder;
using SkyBuilding.Mvc.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Mvc461
{
    public class Startup : JwtStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.UseDependencyInjection();
        }
    }
}
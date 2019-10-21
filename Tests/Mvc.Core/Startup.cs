using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkyBuilding;
using SkyBuilding.Cache;
using SkyBuilding.Config;
using SkyBuilding.Log;
using SkyBuilding.Mvc;
using SkyBuilding.Mvc.Converters;
using SkyBuilding.Serialize.Json;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Mvc.Core
{
    public class Startup : JwtStartup
    {
    }
}

using Newtonsoft.Json.Serialization;
using CodeArts;
using CodeArts.Cache;
using CodeArts.Config;
using CodeArts.Mvc;
using CodeArts.Mvc.Converters;
using CodeArts.Mvc.DependencyInjection;
using CodeArts.Serialize.Json;
using System;
using System.Web.Http;

namespace Mvc4
{
    public class Startup : JwtStartup
    {
    }
}
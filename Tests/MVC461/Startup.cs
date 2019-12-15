using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using CodeArts;
using CodeArts.Cache;
using CodeArts.Mvc;
using CodeArts.Mvc.Builder;
using CodeArts.Mvc.DependencyInjection;
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
    }
}
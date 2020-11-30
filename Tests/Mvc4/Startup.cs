using CodeArts.Mvc;
using CodeArts.Mvc.Builder;

namespace Mvc4
{
    public class Startup : JwtStartup
    {
        public override void Configure(IApplicationBuilder builder)
        {
            base.Configure(builder.MapPost("/test", "/api/values/test"));
        }
    }
}
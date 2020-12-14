using CodeArts;
using CodeArts.Mvc;
using CodeArts.Mvc.Builder;

namespace Mvc4
{
    public class Startup : JwtStartup
    {
        public Startup()
        {
            using (var startup = new XStartup())
            {
                startup.DoStartup();
            }
        }
        public override void Configure(IApplicationBuilder builder)
        {
            base.Configure(builder.MapPost("/test", "/api/values/test"));
        }
    }
}
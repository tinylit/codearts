using CodeArts;
using CodeArts.Mvc;

namespace Mvc45
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
    }
}
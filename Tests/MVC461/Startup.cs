using CodeArts.Mvc;
using System.Web.Http;

namespace Mvc461
{
    /// <summary>
    /// 启动类
    /// </summary>
    public class Startup : JwtStartup
    {
        public override void Configuration(HttpConfiguration config)
        {
            config.BindParameter();

            base.Configuration(config);
        }
    }
}
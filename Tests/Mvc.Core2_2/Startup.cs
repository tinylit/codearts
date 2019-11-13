using Microsoft.Extensions.DependencyInjection;
using SkyBuilding.Mvc;

namespace Mvc.Core2_2
{
    /// <inheritdoc />
    public class Startup : JwtStartup
    {
        /// <summary>
        /// 服务配置（这个方法被运行时调用。使用此方法向容器添加服务。）
        /// </summary>
        /// <param name="services">服务集合</param>
        public override void ConfigureServices(IServiceCollection services)
        {
            //? 依赖注入
            services.UseDependencyInjection();

            base.ConfigureServices(services);
        }
    }
}

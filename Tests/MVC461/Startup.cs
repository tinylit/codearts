using CodeArts;
using CodeArts.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using System.Web.Http;

namespace Mvc461
{
    /// <summary>
    /// 启动类。
    /// </summary>
    public class Startup : JwtStartup
    {
        public Startup()
        {
            using (var startup = new XStartup())
            {
                startup.DoStartup();
            }
        }
        public override void Configuration(HttpConfiguration config)
        {
            base.Configuration(config);

            ModelValidator.CustomValidate<RequiredAttribute>((attr, context) =>
            {
                return $"{context.DisplayName}为必填字段!";
            });
        }

        /// <inheritdoc />
        public void ConfigureServices(IServiceCollection services)
        {
            services.UseDependencyInjection();
        }
    }
}
using CodeArts.Mvc;
using System.ComponentModel.DataAnnotations;
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
            base.Configuration(config);

            ModelValidator.CustomValidate<RequiredAttribute>((attr, context) =>
            {
                return $"{context.DisplayName}为必填字段!";
            });
        }
    }
}
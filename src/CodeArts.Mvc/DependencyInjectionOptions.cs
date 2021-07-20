using Microsoft.Extensions.DependencyInjection;

namespace CodeArts.Mvc
{
    /// <summary>
    /// 依赖注入配置。
    /// </summary>
    public class DependencyInjectionOptions
    {
        /// <summary>
        /// 需要依赖注入的.DLL包过滤规则，默认：*
        /// </summary>
        /// <remarks>缩小依赖注入程序集范围，作为<see cref="System.IO.Directory.GetFiles(string, string)"/>的第二个参数，默认:“*”。</remarks>
        public string Pattern { get; set; } = "*";

        /// <summary>
        /// 最大依赖注入深度，默认：5。
        /// </summary>
        public int MaxDepth { get; set; } = 5;

        /// <summary>
        /// 参数注入的声明周期，默认：<see cref="ServiceLifetime.Scoped"/>。
        /// </summary>
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
    }
}

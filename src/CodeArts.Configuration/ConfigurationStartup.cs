using CodeArts.Config;

namespace CodeArts
{
    /// <summary>
    /// 配置文件启动项。
    /// </summary>
    public class ConfigurationStartup : IStartup
    {
        /// <summary>
        /// 代码（200）。
        /// </summary>
        public int Code => 200;

        /// <summary>
        /// 权重。
        /// </summary>
        public int Weight => 1;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup() => RuntimeServPools.TryAddSingleton<IConfigHelper, DefaultConfigHelper>();
    }
}

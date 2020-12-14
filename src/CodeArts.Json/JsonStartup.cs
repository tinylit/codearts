using CodeArts.Serialize.Json;

namespace CodeArts
{
    /// <summary>
    /// Json 启动器。
    /// </summary>
    public class JsonStartup : IStartup
    {
        /// <summary>
        /// 代码（300）。
        /// </summary>
        public int Code => 300;

        /// <summary>
        /// 权重。
        /// </summary>
        public int Weight => 1;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup() => RuntimeServPools.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();
    }
}

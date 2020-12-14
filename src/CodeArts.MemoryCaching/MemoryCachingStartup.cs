namespace CodeArts.Caching
{
    /// <summary>
    /// 内存启动项。
    /// </summary>
    public class MemoryCachingStartup : IStartup
    {
        /// <summary>
        /// 代码（450）。
        /// </summary>
        public int Code => 450;

        /// <summary>
        /// 权重。
        /// </summary>
        public int Weight => 1;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup() => CachingManager.RegisterProvider(new MemoryCachingProvider(), Level.First);
    }
}

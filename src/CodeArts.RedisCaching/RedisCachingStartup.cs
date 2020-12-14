namespace CodeArts.Caching
{
    /// <summary>
    /// 内存启动项。
    /// </summary>
    public class RedisCachingStartup : IStartup
    {
        /// <summary>
        /// 代码（500）。
        /// </summary>
        public int Code => 500;

        /// <summary>
        /// 权重。
        /// </summary>
        public int Weight => 1;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup() => CachingManager.RegisterProvider(new RedisCachingProvider(), Level.Second);
    }
}

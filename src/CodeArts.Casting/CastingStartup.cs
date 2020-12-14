namespace CodeArts.Casting
{
    /// <summary>
    /// 转换启动项。
    /// </summary>
    public class CastingStartup : IStartup
    {
        /// <summary>
        /// 代码（100）。
        /// </summary>
        public int Code => 100;

        /// <summary>
        /// 权重（1）。
        /// </summary>
        public int Weight => 1;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup() => RuntimeServPools.TryAddSingleton<IMapper, CastingMapper>();
    }
}

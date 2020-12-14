namespace CodeArts
{
    /// <summary>
    /// 启动项（<see cref="XStartup"/>）。
    /// </summary>
    public interface IStartup
    {
        /// <summary>
        /// 代码（相同代码只会使用一个，优先启动代码值更小）。
        /// </summary>
        int Code { get; }

        /// <summary>
        /// 相同【<see cref="Code"/>】时，优先使用权重更高的启动项。
        /// </summary>
        int Weight { get; }

        /// <summary>
        /// 启动。
        /// </summary>
        void Startup();
    }
}

using CodeArts.Proxies.Hooks;

namespace CodeArts.Proxies
{
    /// <summary>
    /// 代理选项
    /// </summary>
    public sealed class ProxyOptions
    {
        /// <summary>
        /// 默认代理配置代理所有方法。
        /// </summary>
        public static readonly ProxyOptions Default = new ProxyOptions(new AllMethodsHook());

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="hook">方法代理的钩子</param>
        public ProxyOptions(IProxyMethodHook hook) => Hook = hook ?? throw new System.ArgumentNullException(nameof(hook));

        /// <summary>
        /// 方法代理钩子
        /// </summary>
        public IProxyMethodHook Hook { get; }

        /// <summary>
        /// 获取一个值，该值标识元数据元素。
        /// </summary>
        /// <value>该值能够唯一标识元数据元素。</value>
        public int MetadataToken => Hook.MetadataToken;
    }
}

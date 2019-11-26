using System.Reflection;

namespace SkyBuilding.Proxies
{
    /// <summary>
    /// 方法代理的钩子
    /// </summary>
    public interface IProxyMethodHook
    {
        /// <summary>
        /// 获取一个值，该值标识元数据元素。
        /// </summary>
        /// <value>该值能够唯一标识元数据元素。</value>
        int MetadataToken { get; }

        /// <summary>
        /// 是否启动指定方法的代理。
        /// </summary>
        /// <param name="info">方法信息</param>
        /// <returns></returns>
        bool IsEnabledFor(MethodInfo info);
    }
}

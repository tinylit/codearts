using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Proxies
{
    /// <summary>
    /// 代理生成器
    /// </summary>
    public interface IProxyGenerator
    {
        /// <summary>
        /// 模块范围。
        /// </summary>
        ModuleScope Scope { get; }

        /// <summary>
        /// 获取指定类型的代理类。
        /// </summary>
        /// <param name="interfaceType">接口</param>
        /// <param name="options">代理配置</param>
        /// <returns></returns>
        TypeBuilder Of(Type interfaceType, ProxyOptions options);

        /// <summary>
        /// 获取指定类型的代理类。
        /// </summary>
        /// <param name="classType">代理类</param>
        /// <param name="options">代理配置</param>
        /// <returns></returns>
        TypeBuilder New(Type classType, ProxyOptions options);
    }
}

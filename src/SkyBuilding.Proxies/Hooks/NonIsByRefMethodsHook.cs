using System.Linq;
using System.Reflection;

namespace SkyBuilding.Proxies.Hooks
{
    /// <summary>
    /// 不包含【ref|out】参数的方法。
    /// </summary>
    public class NonIsByRefMethodsHook : AllMethodsHook
    {
        /// <summary>
        /// 是否启用指定方法的代理。
        /// </summary>
        /// <param name="info">方法信息</param>
        /// <returns></returns>
        public override bool IsEnabledFor(MethodInfo info)
        {
            return base.IsEnabledFor(info) && !info.GetParameters().Any(x => x.ParameterType.IsByRef);
        }
    }
}

using System;

namespace SkyBuilding.Proxies
{
    /// <summary>
    /// 标记不拦截的方法。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class NonInterceptAttribute : Attribute
    {
    }
}

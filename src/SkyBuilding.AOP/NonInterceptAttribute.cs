using System;

namespace SkyBuilding.AOP
{
    /// <summary>
    /// 被标记的方法不会被拦截。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class NonInterceptAttribute : Attribute
    {
    }
}

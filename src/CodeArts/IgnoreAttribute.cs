using System;

namespace CodeArts
{
    /// <summary>
    /// 忽略的键。
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class IgnoreAttribute : Attribute
    {
    }
}

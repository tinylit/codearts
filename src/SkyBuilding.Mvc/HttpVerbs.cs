#if NETSTANDARD2_0 || NETCOREAPP3_0
using System;

namespace SkyBuilding.Mvc
{
    /// <summary>
    /// 请求方式
    /// </summary>
    [Flags]
    public enum HttpVerbs
    {
        Get = 1,
        Post = 2,
        Put = 4,
        Delete = 8,
        Head = 16,
        Patch = 32,
        Options = 64
    }
}
#endif
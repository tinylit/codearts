using System;

namespace CodeArts.Casting
{
    /// <summary>
    /// 映射路由。
    /// </summary>
    public interface IRouter
    {
        /// <summary>
        /// 目标类型。
        /// </summary>
        Type ConversionType { get; }
    }
}

using System;

namespace CodeArts.Casting
{
    /// <summary>
    /// 拷贝配置。
    /// </summary>
    public interface IProfileConfiguration
    {
        /// <summary>
        /// 匹配模式。
        /// </summary>
        PatternKind Kind { get; }

        /// <summary>
        /// 深度映射。
        /// </summary>
        bool? IsDepthMapping { get; }

        /// <summary>
        /// 允许空值传播映射。
        /// </summary>
        bool? AllowNullMapping { get; }
    }
}

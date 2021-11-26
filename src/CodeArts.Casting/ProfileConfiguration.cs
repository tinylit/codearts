using System;

namespace CodeArts.Casting
{
    /// <summary>
    /// 克隆配置。
    /// </summary>
    public sealed class ProfileConfiguration : IProfileConfiguration
    {
        /// <summary>
        /// 匹配模式。
        /// </summary>
        public PatternKind Kind { get; set; } = PatternKind.Property;

        /// <summary>
        /// 深度映射。
        /// </summary>
        public bool? IsDepthMapping { get; set; }

        /// <summary>
        /// 允许空值传播映射。
        /// </summary>
        public bool? AllowNullMapping { get; set; }
    }
}

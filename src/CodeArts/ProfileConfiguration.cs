using System;

namespace CodeArts
{
    /// <summary>
    /// 克隆配置。
    /// </summary>
    public sealed class ProfileConfiguration : IProfileConfiguration
    {
        /// <summary>
        /// 类型创建器。
        /// </summary>
        public Func<Type, object> ServiceCtor { get; set; } = Activator.CreateInstance;

        /// <summary>
        /// 匹配模式。
        /// </summary>
        public PatternKind Kind { get; set; } = PatternKind.Property;

        /// <summary>
        /// 深度映射。
        /// </summary>
        public bool? IsDepthMapping { get; set; }

        /// <summary>
        /// 允许空目标值。
        /// </summary>
        public bool? AllowNullDestinationValues { get; set; }

        /// <summary>
        /// 允许空值传播映射。
        /// </summary>
        public bool? AllowNullPropagationMapping { get; set; }
    }
}

using System;

namespace SkyBuilding
{
    /// <summary>
    /// 拷贝配置
    /// </summary>
    public interface IProfileConfiguration
    {
        /// <summary>
        /// 类型创建器
        /// </summary>
        Func<Type, object> ServiceCtor { get; }

        /// <summary>
        /// 匹配模式
        /// </summary>
        PatternKind Kind { get; }

        /// <summary>
        /// 允许空目标值。
        /// </summary>
        bool? AllowNullDestinationValues { get; }

        /// <summary>
        /// 允许空值传播映射。
        /// </summary>
        bool? AllowNullPropagationMapping { get; }
    }
}

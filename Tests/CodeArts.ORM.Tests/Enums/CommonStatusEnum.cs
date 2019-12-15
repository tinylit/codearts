using System.ComponentModel;

namespace UnitTest.Enums
{
    /// <summary>
    /// 公共状态枚举
    /// </summary>
    public enum CommonStatusEnum
    {
        /// <summary>
        /// 删除
        /// </summary>
        [Description("已删除")]
        Deleted = -1,
        /// <summary>
        /// 未激活
        /// </summary>
        [Description("未激活")]
        NonActivated = 0,
        /// <summary>
        /// 启用
        /// </summary>
        [Description("启用")]
        Enabled = 1,
        /// <summary>
        /// 禁用
        /// </summary>
        [Description("禁用")]
        Disabled = 2
    }
}

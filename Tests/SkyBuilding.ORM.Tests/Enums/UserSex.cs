using System.ComponentModel;

namespace UnitTest.Enums
{
    /// <summary>
    /// 用户性别
    /// </summary>
    public enum UserSex
    {
        /// <summary>
        /// 未知
        /// </summary>
        [Description("未知")]
        Unkown = 0,
        /// <summary>
        /// 男
        /// </summary>
        [Description("男")]
        Male = 1 << 1,
        /// <summary>
        /// 女
        /// </summary>
        [Description("女")]
        Female = 1 << 2,
        /// <summary>
        /// 中性
        /// </summary>
        [Description("中性")]
        Neutral = 1 << 3
    }
}

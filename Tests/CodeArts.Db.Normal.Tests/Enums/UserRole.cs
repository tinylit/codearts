using System.ComponentModel;

namespace UnitTest.Enums
{
    /// <summary>
    /// 用户角色。
    /// </summary>
    public enum UserRole
    {
        #region 客户
        /// <summary>
        /// 用户。
        /// </summary>
        [Description("用户")]
        Normal = 1 << 0,
        /// <summary>
        /// 管理员。
        /// </summary>
        [Description("管理员")]
        Administrator = 1 << 1,
        #endregion

        #region 系统
        /// <summary>
        /// 销售人员。
        /// </summary>
        [Description("销售人")]
        Saler = 1 << 2,
        /// <summary>
        /// 开发者。
        /// </summary>
        [Description("开发者")]
        Developer = 1 << 3,
        /// <summary>
        /// 维护人。
        /// </summary>
        [Description("维护人")]
        Maintainer = 1 << 4,
        /// <summary>
        /// 拥有者。
        /// </summary>
        [Description("拥有者")]
        Owner = 1 << 5
        #endregion

    }
}

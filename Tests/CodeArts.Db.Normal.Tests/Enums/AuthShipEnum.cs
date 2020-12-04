using System.ComponentModel;

namespace UnitTest.Enums
{
    /// <summary>
    /// 权限关系类型。
    /// </summary>
    public enum AuthShipEnum
    {
        /// <summary>
        /// 人员权限。
        /// </summary>
        [Description("人员权限")]
        User = 1 << 0,
        /// <summary>
        /// 机构权限。
        /// </summary>
        [Description("机构权限")]
        Tree = 1 << 1,
        /// <summary>
        /// 付费权限。
        /// </summary>
        [Description("付费权限")]
        Vip = 1 << 2
    }
}

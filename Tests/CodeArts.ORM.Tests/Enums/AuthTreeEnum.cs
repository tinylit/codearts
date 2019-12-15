namespace UnitTest.Enums
{
    /// <summary>
    /// 权限类型
    /// </summary>
    public enum AuthTreeEnum
    {
        /// <summary>
        /// 项目
        /// </summary>
        Project = 1 << 0,
        /// <summary>
        /// 导航
        /// </summary>
        Nav = 1 << 1,
        /// <summary>
        /// 菜单
        /// </summary>
        Menu = 1 << 2,
        /// <summary>
        /// 页面
        /// </summary>
        Page = 1 << 3,
        /// <summary>
        /// 功能
        /// </summary>
        Function = 1 << 4,
        /// <summary>
        /// 面板
        /// </summary>
        Panel = 1 << 5,
        /// <summary>
        /// 提示
        /// </summary>
        Alert = 1 << 6,
        /// <summary>
        /// 标记
        /// </summary>
        Label = 1 << 7
    }
}

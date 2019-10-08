namespace UnitTest.Enums
{
    /// <summary>
    /// 机构类型
    /// </summary>
    public enum OrgTreeEnum
    {
        /// <summary>
        /// 集团
        /// </summary>
        G = 1 << 0,
        /// <summary>
        /// 单位
        /// </summary>
        N = 1 << 1,
        /// <summary>
        /// 部门
        /// </summary>
        M = 1 << 2,
        /// <summary>
        /// 商铺
        /// </summary>
        S = 1 << 3,
        /// <summary>
        /// 虚拟节点
        /// </summary>
        V = 1 << 4
    }
}

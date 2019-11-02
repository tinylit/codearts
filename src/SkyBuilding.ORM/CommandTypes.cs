namespace SkyBuilding.ORM
{
    /// <summary>
    /// 命令
    /// </summary>
    public static class CommandTypes
    {
        /// <summary>
        /// 增加
        /// </summary>
        public readonly static UppercaseString Insert = new UppercaseString("INSERT");

        /// <summary>
        /// 删除
        /// </summary>
        public readonly static UppercaseString Delete = new UppercaseString("DELETE");

        /// <summary>
        /// 查询
        /// </summary>
        public readonly static UppercaseString Select = new UppercaseString("SELECT");

        /// <summary>
        /// 更新
        /// </summary>
        public readonly static UppercaseString Update = new UppercaseString("UPDATE");

        /// <summary>
        /// 创建
        /// </summary>
        public readonly static UppercaseString Create = new UppercaseString("CREATE");

        /// <summary>
        /// 更改
        /// </summary>
        public readonly static UppercaseString Alter = new UppercaseString("ALTER");

        /// <summary>
        /// 移除
        /// </summary>
        public readonly static UppercaseString Drop = new UppercaseString("DROP");
    }
}

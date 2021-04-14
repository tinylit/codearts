namespace CodeArts.Db
{
    /// <summary>
    /// 命令。
    /// </summary>
    public static class CommandTypes
    {
        /// <summary>
        /// 增加。
        /// </summary>
        public static readonly UppercaseString Insert = new UppercaseString("INSERT");

        /// <summary>
        /// 删除。
        /// </summary>
        public static readonly UppercaseString Delete = new UppercaseString("DELETE");

        /// <summary>
        /// 查询。
        /// </summary>
        public static readonly UppercaseString Select = new UppercaseString("SELECT");

        /// <summary>
        /// 更新。
        /// </summary>
        public static readonly UppercaseString Update = new UppercaseString("UPDATE");

        /// <summary>
        /// 创建。
        /// </summary>
        public static readonly UppercaseString Create = new UppercaseString("CREATE");

        /// <summary>
        /// 更改。
        /// </summary>
        public static readonly UppercaseString Alter = new UppercaseString("ALTER");

        /// <summary>
        /// 移除。
        /// </summary>
        public static readonly UppercaseString Drop = new UppercaseString("DROP");
    }
}

namespace CodeArts.ORM
{
    /// <summary>
    /// 执行行为。
    /// </summary>
    public enum CommandBehavior
    {
        /// <summary>
        /// 查询。
        /// </summary>
        Select = 0,
        /// <summary>
        /// 更新
        /// </summary>
        Update = 1,
        /// <summary>
        /// 删除
        /// </summary>
        Delete = 2,
        /// <summary>
        /// 插入
        /// </summary>
        Insert = 3
    }
}

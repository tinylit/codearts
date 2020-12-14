namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据库链接适配器。
    /// </summary>
    public interface IDbConnectionLtsAdapter : IDbConnectionAdapter
    {
        /// <summary>
        /// 访问器。
        /// </summary>
        ICusomVisitorCollect Visitors { get; }
    }
}

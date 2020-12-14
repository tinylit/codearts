namespace CodeArts.Db
{
    /// <summary>
    /// 数据库链接适配器。
    /// </summary>
    public interface IDbConnectionAdapter : IDbConnectionFactory
    {
        /// <summary>
        /// SQL矫正设置。
        /// </summary>
        ISQLCorrectSettings Settings { get; }
    }
}

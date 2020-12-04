namespace CodeArts.Db
{
    /// <summary>
    /// 数据库链接适配器。
    /// </summary>
    public interface IDbConnectionSimAdapter : IDbConnectionFactory
    {
        /// <summary>
        /// SQL矫正设置。
        /// </summary>
        ISQLCorrectSimSettings SimSettings { get; }
    }
}

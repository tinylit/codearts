namespace CodeArts.Db
{
    /// <summary>
    /// Sqlite 适配器。
    /// </summary>
    public class SqliteAdapter : SqliteFactory, IDbConnectionAdapter, IDbConnectionFactory
    {
        /// <summary>
        /// 矫正配置。
        /// </summary>
        public virtual ISQLCorrectSettings Settings => Singleton<SqliteCorrectSettings>.Instance;
    }
}

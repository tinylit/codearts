namespace CodeArts.Db
{
    /// <summary>
    /// MySQL 适配器。
    /// </summary>
    public class MySqlAdapter : MySqlFactory, IDbConnectionAdapter, IDbConnectionFactory
    {
        /// <summary>
        /// 矫正配置。
        /// </summary>
        public virtual ISQLCorrectSettings Settings => Singleton<MySqlCorrectSettings>.Instance;
    }
}

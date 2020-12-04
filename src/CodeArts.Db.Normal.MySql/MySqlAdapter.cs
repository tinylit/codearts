namespace CodeArts.Db.MySql
{
    /// <summary>
    /// MySQL 适配器。
    /// </summary>
    public class MySqlAdapter : MySqlSimAdapter, IDbConnectionAdapter, IDbConnectionSimAdapter, IDbConnectionFactory
    {
        /// <summary>
        /// 矫正配置。
        /// </summary>
        public virtual ISQLCorrectSettings Settings => Singleton<MySqlCorrectSettings>.Instance;
    }
}

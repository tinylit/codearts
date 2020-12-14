namespace CodeArts.Db
{
    /// <summary>
    /// SqlServer 适配器。
    /// </summary>
    public class SqlServerAdapter : SqlServerFactory, IDbConnectionAdapter, IDbConnectionFactory
    {
        /// <summary>
        /// 矫正配置。
        /// </summary>
        public virtual ISQLCorrectSettings Settings => Singleton<SqlServerCorrectSettings>.Instance;
    }
}

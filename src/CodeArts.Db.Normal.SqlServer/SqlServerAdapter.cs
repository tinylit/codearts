namespace CodeArts.Db.SqlServer
{
    /// <summary>
    /// SqlServer 适配器。
    /// </summary>
    public class SqlServerAdapter : SqlServerSimAdapter, IDbConnectionAdapter, IDbConnectionSimAdapter, IDbConnectionFactory
    {
        /// <summary>
        /// 矫正配置。
        /// </summary>
        public virtual ISQLCorrectSettings Settings => Singleton<SqlServerCorrectSettings>.Instance;
    }
}

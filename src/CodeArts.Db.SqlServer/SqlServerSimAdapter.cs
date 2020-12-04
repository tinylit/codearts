namespace CodeArts.Db.SqlServer
{
    /// <summary>
    /// SqlServer 适配器。
    /// </summary>
    public class SqlServerSimAdapter : SqlServerFactory, IDbConnectionSimAdapter, IDbConnectionFactory
    {
        /// <summary>
        /// 矫正配置。
        /// </summary>
        public virtual ISQLCorrectSimSettings SimSettings => Singleton<SqlServerCorrectSimSettings>.Instance;
    }
}

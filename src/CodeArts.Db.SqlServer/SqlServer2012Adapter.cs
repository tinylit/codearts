namespace CodeArts.Db
{
    /// <summary>
    /// SqlServer 适配器。
    /// </summary>
    public class SqlServer2012Adapter : SqlServerFactory, IDbConnectionAdapter, IDbConnectionFactory
    {
        /// <summary>
        /// 矫正配置。
        /// </summary>
        public virtual ISQLCorrectSettings Settings => Singleton<SqlServer2012CorrectSettings>.Instance;
    }
}

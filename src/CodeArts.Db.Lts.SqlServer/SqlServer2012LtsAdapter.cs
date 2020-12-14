namespace CodeArts.Db.Lts
{
    /// <summary>
    /// SqlServer 2012 适配器（OFFSET/FETCH NEXT）。
    /// </summary>
    public class SqlServer2012LtsAdapter : SqlServer2012Adapter, IDbConnectionLtsAdapter, IDbConnectionAdapter, IDbConnectionFactory
    {
        private CusomVisitorCollect visitters;

        /// <summary>
        /// 格式化。
        /// </summary>
        public ICusomVisitorCollect Visitors => visitters ?? (visitters = new CusomVisitorCollect());
    }
}

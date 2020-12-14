namespace CodeArts.Db.Lts
{
    /// <summary>
    /// SqlServer 适配器。
    /// </summary>
    public class SqlServerLtsAdapter : SqlServerAdapter, IDbConnectionLtsAdapter, IDbConnectionAdapter, IDbConnectionFactory
    {
        private CusomVisitorCollect visitters;

        /// <summary>
        /// 格式化。
        /// </summary>
        public ICusomVisitorCollect Visitors => visitters ?? (visitters = new CusomVisitorCollect());
    }
}

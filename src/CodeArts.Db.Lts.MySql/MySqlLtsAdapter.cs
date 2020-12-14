namespace CodeArts.Db.Lts
{
    /// <summary>
    /// MySQL 适配器。
    /// </summary>
    public class MySqlLtsAdapter : MySqlAdapter, IDbConnectionLtsAdapter, IDbConnectionAdapter, IDbConnectionFactory
    {
        private CusomVisitorCollect visitters;

        /// <summary>
        /// 格式化。
        /// </summary>
        public ICusomVisitorCollect Visitors => visitters ?? (visitters = new CusomVisitorCollect());
    }
}

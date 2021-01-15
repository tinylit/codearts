namespace CodeArts.Db.Lts
{
    /// <summary>
    /// MySQL 适配器。
    /// </summary>
    public class MySqlLtsAdapter : MySqlAdapter, IDbConnectionLtsAdapter, IDbConnectionAdapter, IDbConnectionFactory
    {
        private CustomVisitorList visitters;

        /// <summary>
        /// 格式化。
        /// </summary>
        public ICustomVisitorList Visitors => visitters ?? (visitters = new CustomVisitorList());
    }
}

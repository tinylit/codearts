namespace CodeArts.Db.Lts
{
    /// <summary>
    /// SqlServer 适配器。
    /// </summary>
    public class SqlServerLtsAdapter : SqlServerAdapter, IDbConnectionLtsAdapter, IDbConnectionAdapter, IDbConnectionFactory
    {
        private CustomVisitorList visitters;

        /// <summary>
        /// 格式化。
        /// </summary>
        public ICustomVisitorList Visitors => visitters ?? (visitters = new CustomVisitorList());
    }
}

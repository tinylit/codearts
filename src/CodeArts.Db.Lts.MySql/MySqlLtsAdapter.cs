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
        #if NETSTANDARD2_1_OR_GREATER
        public ICustomVisitorList Visitors => visitters ??= new CustomVisitorList();
#else
        public ICustomVisitorList Visitors => visitters ?? (visitters = new CustomVisitorList());
#endif
    }
}

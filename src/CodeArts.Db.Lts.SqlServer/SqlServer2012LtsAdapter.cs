namespace CodeArts.Db.Lts
{
    /// <summary>
    /// SqlServer 2012 适配器（OFFSET/FETCH NEXT）。
    /// </summary>
    public class SqlServer2012LtsAdapter : SqlServer2012Adapter, IDbConnectionLtsAdapter, IDbConnectionAdapter, IDbConnectionFactory
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

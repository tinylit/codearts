using CodeArts.Db;
using CodeArts.Db.SqlServer;

namespace CodeArts.SqlServer
{
    /// <summary>
    /// SqlServer 2012 适配器（OFFSET/FETCH NEXT）。
    /// </summary>
    public class SqlServer2012Adapter : SqlServerAdapter
    {
        /// <summary>
        /// 矫正配置。
        /// </summary>
        public override ISQLCorrectSettings Settings => Singleton<SqlServer2012CorrectSettings>.Instance;
    }
}

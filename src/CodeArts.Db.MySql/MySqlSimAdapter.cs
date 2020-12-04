using CodeArts.Db;

namespace CodeArts.Db.MySql
{
    /// <summary>
    /// MySQL 适配器。
    /// </summary>
    public class MySqlSimAdapter : MySqlFactory, IDbConnectionSimAdapter, IDbConnectionFactory
    {
        /// <summary>
        /// 矫正配置。
        /// </summary>
        public virtual ISQLCorrectSimSettings SimSettings => Singleton<MySqlCorrectSimSettings>.Instance;

    }
}

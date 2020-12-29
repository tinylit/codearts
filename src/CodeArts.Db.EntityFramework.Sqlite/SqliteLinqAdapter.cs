using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite.Infrastructure.Internal;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// SqlServer 适配器。
    /// </summary>
    public class SqliteLinqAdapter : SqliteAdapter, IDbConnectionLinqAdapter, IDbConnectionAdapter, IDbConnectionFactory
    {
        /// <summary>
        /// 配置SqlServer支持。
        /// </summary>
        /// <param name="optionsBuilder">配置器。</param>
        /// <param name="connectionConfig">链接配置。</param>
        public void OnConfiguring(DbContextOptionsBuilder optionsBuilder, IReadOnlyConnectionConfig connectionConfig)
        {
            optionsBuilder.UseSqlite(connectionConfig.ConnectionString);
        }
    }
}

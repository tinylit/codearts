#if NET_CORE
using Microsoft.EntityFrameworkCore;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 数据连接适配器。
    /// </summary>
    public interface IDbConnectionLinqAdapter : IDbConnectionAdapter
    {
        /// <summary>
        /// 连接配置。
        /// </summary>
        /// <param name="optionsBuilder">构造器。</param>
        /// <param name="connectionConfig">数据库连接配置。</param>
        void OnConfiguring(DbContextOptionsBuilder optionsBuilder, IReadOnlyConnectionConfig connectionConfig);
    }
}
#endif
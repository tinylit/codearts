#if NETSTANDARD2_0
using Microsoft.EntityFrameworkCore;
using System;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 数据连接适配器。
    /// </summary>
    public interface IDbConnectionLinqAdapter : IDbConnectionAdapter
    {
        /// <summary>
        /// 关系配置扩展类型。
        /// </summary>
        Type RelationalOptionsExtensionType { get; }

        /// <summary>
        /// 连接配置。
        /// </summary>
        /// <param name="optionsBuilder">构造器。</param>
        /// <param name="connectionConfig">数据库连接配置。</param>
        void OnConfiguring(DbContextOptionsBuilder optionsBuilder, IReadOnlyConnectionConfig connectionConfig);
    }
}
#endif
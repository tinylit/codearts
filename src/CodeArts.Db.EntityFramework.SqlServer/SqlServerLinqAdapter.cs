﻿using Microsoft.EntityFrameworkCore;
using System;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// SqlServer 适配器。
    /// </summary>
    public class SqlServerLinqAdapter : SqlServerAdapter, IDbConnectionLinqAdapter, IDbConnectionAdapter, IDbConnectionFactory
    {
        /// <summary>
        /// <see cref="SqlServerTaskOptionsExtension"/>
        /// </summary>
        public Type RelationalOptionsExtensionType => typeof(SqlServerTaskOptionsExtension);

        /// <summary>
        /// 配置SqlServer支持。
        /// </summary>
        /// <param name="optionsBuilder">配置器。</param>
        /// <param name="connectionConfig">链接配置。</param>
        public void OnConfiguring(DbContextOptionsBuilder optionsBuilder, IReadOnlyConnectionConfig connectionConfig)
        {
            optionsBuilder.UseSqlServer(connectionConfig.ConnectionString);
        }
    }
}

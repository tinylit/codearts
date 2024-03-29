﻿#if NETSTANDARD2_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
#else
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#endif
#if NETSTANDARD2_0_OR_GREATER || NET45_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
#endif
using System.Collections.Concurrent;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 数据库上下文。
    /// </summary>
    public class DbContext<TDbContext> : DbContext where TDbContext : DbContext<TDbContext>
    {
        private readonly IReadOnlyConnectionConfig connectionConfig;
        private static readonly ConcurrentDictionary<Type, DbConfigAttribute> DbConfigCache = new ConcurrentDictionary<Type, DbConfigAttribute>();

#if NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// inheritdoc
        /// </summary>
        public DbContext() : base(new DbContextOptions<TDbContext>())
        {
            this.connectionConfig = GetDbConfig() ?? throw new ArgumentNullException(nameof(connectionConfig)); ;
        }
        /// <summary>
        /// inheritdoc
        /// </summary>
        public DbContext(IReadOnlyConnectionConfig connectionConfig) : base(new DbContextOptions<TDbContext>())
        {
            this.connectionConfig = connectionConfig ?? throw new ArgumentNullException(nameof(connectionConfig));
        }
#else
        /// <summary>
        /// inheritdoc
        /// </summary>
        public DbContext() : this(StaticGetDbConfig())
        {
        }

        /// <summary>
        /// inheritdoc
        /// </summary>
        public DbContext(IReadOnlyConnectionConfig connectionConfig) : base(connectionConfig?.ConnectionString)
        {
            this.connectionConfig = connectionConfig ?? throw new ArgumentNullException(nameof(connectionConfig));
        }
#endif

        /// <summary>
        /// 获取数据库配置。
        /// </summary>
        /// <returns></returns>
        private static IReadOnlyConnectionConfig StaticGetDbConfig()
        {
            var attr = DbConfigCache.GetOrAdd(typeof(TDbContext), type =>
            {
                return (DbConfigAttribute)Attribute.GetCustomAttribute(type, typeof(DbConfigAttribute));
            });

            return attr.GetConfig();
        }

        /// <summary>
        /// 获取数据库配置。
        /// </summary>
        /// <returns></returns>
        protected virtual IReadOnlyConnectionConfig GetDbConfig() => StaticGetDbConfig();

        /// <summary> 连接名称。 </summary>
        public string Name => connectionConfig.Name;

        /// <summary> 数据库驱动名称。 </summary>
        public string ProviderName => connectionConfig.ProviderName;

        /// <summary>
        /// SQL 矫正。
        /// </summary>
        public ISQLCorrectSettings Settings => DbAdapter.Settings;

#if NETSTANDARD2_0_OR_GREATER
        private IDbConnectionLinqAdapter connectionAdapter;

        /// <summary>
        /// 适配器。
        /// </summary>
        protected IDbConnectionLinqAdapter DbAdapter
        {
            get
            {
                if (connectionAdapter is null || !string.Equals(connectionAdapter.ProviderName, connectionConfig.ProviderName))
                {
                    connectionAdapter = CreateDbAdapter(connectionConfig.ProviderName);
                }

                return connectionAdapter;
            }
        }

        /// <summary>
        /// 创建适配器。
        /// </summary>
        /// <returns></returns>
        protected virtual IDbConnectionLinqAdapter CreateDbAdapter(string providerName) => LinqConnectionManager.Get(providerName);
#else

        private IDbConnectionAdapter connectionAdapter;

        /// <summary>
        /// 适配器。
        /// </summary>
        protected IDbConnectionAdapter DbAdapter
        {
            get
            {
                if (connectionAdapter is null || !string.Equals(connectionAdapter.ProviderName, connectionConfig.ProviderName))
                {
                    connectionAdapter = CreateDbAdapter(connectionConfig.ProviderName);
                }

                return connectionAdapter;
            }
        }


        /// <summary>
        /// 创建适配器。
        /// </summary>
        /// <returns></returns>
        protected virtual IDbConnectionAdapter CreateDbAdapter(string providerName) => LinqConnectionManager.Get(providerName);
#endif

#if NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// inheritdoc
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            DbAdapter.OnConfiguring(optionsBuilder, connectionConfig);

            base.OnConfiguring(optionsBuilder);
        }
#endif

        /// <summary>
        /// 从SQL语句中获取查询器。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="parameters">参数。</param>
        /// <returns></returns>
#if NETSTANDARD2_0_OR_GREATER
        public virtual IQueryable<TEntity> FromSql<TEntity>(SQL sql, params object[] parameters) where TEntity : class
            => Set<TEntity>().FromSqlRaw(sql.ToString(Settings), parameters);

#else
        public virtual DbRawSqlQuery<TEntity> FromSql<TEntity>(SQL sql, params object[] parameters) where TEntity : class
            => Database.SqlQuery<TEntity>(sql.ToString(Settings), parameters);
#endif

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="parameters">参数。</param>
        /// <returns></returns>
        public virtual int ExecuteCommand(SQL sql, params object[] parameters)
#if NETSTANDARD2_0_OR_GREATER
            => Database.ExecuteSqlRaw(sql.ToString(Settings), parameters);
#else
            => Database.ExecuteSqlCommand(sql.ToString(Settings), parameters);
#endif

#if NETSTANDARD2_0_OR_GREATER || NET45_OR_GREATER

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="parameters">参数。</param>
        /// <returns></returns>
        public virtual Task<int> ExecuteCommandAsync(SQL sql, params object[] parameters)
#if NETSTANDARD2_0_OR_GREATER
            => Database.ExecuteSqlRawAsync(sql.ToString(Settings), parameters);
#else
            => Database.ExecuteSqlCommandAsync(sql.ToString(Settings), parameters);
#endif

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <param name="parameters">参数。</param>
        /// <returns></returns>
        public virtual Task<int> ExecuteCommandAsync(SQL sql, CancellationToken cancellationToken, params object[] parameters)
#if NETSTANDARD2_0_OR_GREATER
            => Database.ExecuteSqlRawAsync(sql.ToString(Settings), parameters ?? Enumerable.Empty<object>(), cancellationToken);
#else
            => Database.ExecuteSqlCommandAsync(sql.ToString(Settings), cancellationToken, parameters);
#endif
#endif
    }
}
#if NETSTANDARD2_0
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
#else
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#endif
#if NETSTANDARD2_0 || NET_NORMAL
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

#if NETSTANDARD2_0
        private class DbContextNestedOptions : DbContextOptions
        {
            private readonly IReadOnlyDictionary<Type, IDbContextOptionsExtension> extensions;

            public DbContextNestedOptions(IReadOnlyDictionary<Type, IDbContextOptionsExtension> extensions) : base(extensions)
            {
                this.extensions = extensions;
            }

            public override Type ContextType => typeof(TDbContext);

            public override DbContextOptions WithExtension<TExtension>(TExtension extension)
            {
                if (extension is null)
                {
                    throw new ArgumentNullException(nameof(extension));
                }

                bool flag = true;

                var type = typeof(TExtension);
                
                var dic = new Dictionary<Type, IDbContextOptionsExtension>();

                foreach (var kv in this.extensions)
                {
                    if (type == kv.Key)
                    {
                        flag = false;

                        dic.Add(kv.Key, extension);
                    }
                    else
                    {
                        dic.Add(kv.Key, kv.Value);
                    }
                }

                if (flag)
                {
                    dic.Add(type, extension);
                }

                return new DbContextNestedOptions(dic);
            }
        }
        /// <summary>
        /// inheritdoc
        /// </summary>
        public DbContext() : this(GetDbConfig())
        {
        }
        /// <summary>
        /// inheritdoc
        /// </summary>
        public DbContext(IReadOnlyConnectionConfig connectionConfig) : base(new DbContextOptions<DbContext<TDbContext>>())
        {
            this.connectionConfig = connectionConfig ?? throw new ArgumentNullException(nameof(connectionConfig));
        }
#else
        /// <summary>
        /// inheritdoc
        /// </summary>
        public DbContext() : this(GetDbConfig())
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
        private static IReadOnlyConnectionConfig GetDbConfig()
        {
            var attr = DbConfigCache.GetOrAdd(typeof(TDbContext), type =>
            {
                return (DbConfigAttribute)Attribute.GetCustomAttribute(type, typeof(DbConfigAttribute));
            });

            return attr.GetConfig();
        }

        /// <summary> 连接名称。 </summary>
        public string Name => connectionConfig.Name;

        /// <summary> 数据库驱动名称。 </summary>
        public string ProviderName => connectionConfig.ProviderName;

        /// <summary>
        /// SQL 矫正。
        /// </summary>
        public ISQLCorrectSettings Settings => DbAdapter.Settings;

#if NETSTANDARD2_0
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

#if NETSTANDARD2_0
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
#if NETSTANDARD2_0
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
#if NETSTANDARD2_0
            => Database.ExecuteSqlRaw(sql.ToString(Settings), parameters);
#else
            => Database.ExecuteSqlCommand(sql.ToString(Settings), parameters);
#endif

#if NETSTANDARD2_0 || NET_NORMAL

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="parameters">参数。</param>
        /// <returns></returns>
        public virtual Task<int> ExecuteCommandAsync(SQL sql, params object[] parameters)
#if NETSTANDARD2_0
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
        public virtual Task<int> ExecuteCommandAsync(SQL sql, CancellationToken cancellationToken = default, params object[] parameters)
#if NETSTANDARD2_0
            => Database.ExecuteSqlRawAsync(sql.ToString(Settings), cancellationToken, parameters);
#else
            => Database.ExecuteSqlCommandAsync(sql.ToString(Settings), cancellationToken, parameters);
#endif
#endif
    }
}
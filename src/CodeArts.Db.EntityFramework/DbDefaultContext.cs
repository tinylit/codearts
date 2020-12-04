#if NETSTANDARD2_0
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
#else
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#endif
#if NETSTANDARD2_0 || !NET40
using System.Threading;
using System.Threading.Tasks;
#endif
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Internal;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 数据库上下文。
    /// </summary>
    public class DbDefaultContext<TEntity> : DbContext, IDbSetCache where TEntity : class, IEntiy
    {
        private readonly IReadOnlyConnectionConfig connectionConfig;


        /// <summary>
        /// inheritdoc
        /// </summary>
#if NETSTANDARD2_0
        public DbDefaultContext(IReadOnlyConnectionConfig connectionConfig) : base(ReadyOptions(connectionConfig))
        {
            this.connectionConfig = connectionConfig ?? throw new System.ArgumentNullException(nameof(connectionConfig));
        }

        private IDictionary<Type, object> _sets;
        private static readonly ConcurrentDictionary<string, CoreOptionsExtension> OptionsCache = new ConcurrentDictionary<string, CoreOptionsExtension>();
        private static DbContextOptions<DbDefaultContext<TEntity>> ReadyOptions(IReadOnlyConnectionConfig connectionConfig)
        {
            if (connectionConfig is null)
            {
                throw new ArgumentNullException(nameof(connectionConfig));
            }

            var dic = new Dictionary<Type, IDbContextOptionsExtension>
            {
                [typeof(CoreOptionsExtension)] = OptionsCache.GetOrAdd(connectionConfig.ConnectionString, _ => new CoreOptionsExtension())
            };

            return new DbContextOptions<DbDefaultContext<TEntity>>(dic);
        }

        object IDbSetCache.GetOrAddSet(IDbSetSource source, Type type)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_sets == null)
            {
                _sets = new Dictionary<Type, object>();
            }

            if (!_sets.TryGetValue(type, out var set))
            {
                set = source.Create(this, type);
                _sets[type] = set;
            }

            return set;
        }
#else
        public DbDefaultContext(IReadOnlyConnectionConfig connectionConfig) : base(connectionConfig?.ConnectionString)
        {
            this.connectionConfig = connectionConfig ?? throw new System.ArgumentNullException(nameof(connectionConfig));
        }
#endif

        /// <summary> 连接名称。 </summary>
        public string Name => connectionConfig.Name;

        /// <summary> 数据库驱动名称。 </summary>
        public string ProviderName => connectionConfig.ProviderName;

        /// <summary>
        /// SQL 矫正。
        /// </summary>
        public ISQLCorrectSimSettings Settings => DbAdapter.SimSettings;

        /// <summary>
        /// EFCore 创建需要。
        /// </summary>
        DbSet<TEntity> DbSets { get; set; }

#if NETSTANDARD2_0
        private DbSet<TEntity> _dbSet;

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
        protected virtual IDbConnectionAdapter CreateDbAdapter(string providerName) => DbConnectionManager.Get(providerName);
#else

        private IDbConnectionSimAdapter connectionAdapter;

        /// <summary>
        /// 适配器。
        /// </summary>
        protected IDbConnectionSimAdapter DbAdapter
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
        protected virtual IDbConnectionSimAdapter CreateDbAdapter(string providerName) => DbConnectionManager.Get(providerName);
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
        public IQueryable<TEntity> FromSql(SQL sql, params object[] parameters)
        {
            if (_dbSet is null)
            {
                _dbSet = Set<TEntity>();
            }

            return _dbSet.FromSqlRaw(sql.ToString(Settings), parameters);
        }
#else
        public DbRawSqlQuery<TEntity> FromSql<TEntity>(SQL sql, params object[] parameters) => Database.SqlQuery<TEntity>(sql.ToString(Settings), parameters);
#endif

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="parameters">参数。</param>
        /// <returns></returns>
        public int ExecuteCommand(SQL sql, params object[] parameters)
#if NETSTANDARD2_0
            => Database.ExecuteSqlRaw(sql.ToString(Settings), parameters);
#else
            => Database.ExecuteSqlCommand(sql.ToString(Settings), parameters);
#endif

#if NETSTANDARD2_0 || !NET40

        /// <summary>
        /// 执行语句。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="parameters">参数。</param>
        /// <returns></returns>
        public Task<int> ExecuteCommandAsync(SQL sql, params object[] parameters)
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
        public Task<int> ExecuteCommandAsync(SQL sql, CancellationToken cancellationToken = default, params object[] parameters)
#if NETSTANDARD2_0
            => Database.ExecuteSqlRawAsync(sql.ToString(Settings), cancellationToken, parameters);
#else
            => Database.ExecuteSqlCommandAsync(sql.ToString(Settings), cancellationToken, parameters);
#endif
#endif
    }
}
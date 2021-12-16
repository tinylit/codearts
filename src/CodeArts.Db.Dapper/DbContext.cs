using CodeArts.Db.Exceptions;
using CodeArts.Runtime;
using Dapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Db.Dapper
{
    /// <summary>
    /// 数据库访问。
    /// </summary>
    public enum DbAccess
    {
        /// <summary>
        /// 自动。
        /// </summary>
        Automatic,
        /// <summary>
        /// 只读库。
        /// </summary>
        Read,
        /// <summary>
        /// 可读写库。
        /// </summary>
        ReadWrite
    }

    /// <summary>
    /// 数据上下文。
    /// </summary>
    public class DbContext : IDisposable
    {
        const string TransgressionError = "部分操作表不在上下文中!";
        const string IllegalReadError = "只允许查询操作!";
        const string IllegalWriteError = "未识别到写操作!";

        private static readonly ConcurrentDictionary<Type, DbConfigAttribute> DbMasterConfigCache = new ConcurrentDictionary<Type, DbConfigAttribute>();
        private static readonly ConcurrentDictionary<Type, DbConfigAttribute> DbSlaveConfigCache = new ConcurrentDictionary<Type, DbConfigAttribute>();
        private static readonly ConcurrentDictionary<Type, DbTablesEngine> DbTables = new ConcurrentDictionary<Type, DbTablesEngine>();

        private readonly bool initialized;
        private readonly DbAccess dbAccess;
        private readonly TablesEngine tablesEngine;
        private readonly IReadOnlyConnectionConfig connectionConfig;
        private readonly IReadOnlyConnectionConfig connectionSlaveConfig;

        private class TablesEngine
        {
            private readonly HashSet<string> tables;

            public TablesEngine(HashSet<string> tables)
            {
                this.tables = tables;
            }

            public bool IsValid(string table) => tables.Contains(table);
        }

        private class DbTablesEngine
        {
            private readonly HashSet<string> tables;
            private readonly Action<DbContext> initialize;

            public DbTablesEngine(Action<DbContext> initialize, HashSet<string> tables)
            {
                this.initialize = initialize;
                this.tables = tables;
            }

            public TablesEngine Initialize(DbContext context)
            {
                initialize.Invoke(context);

                return new TablesEngine(tables);
            }
        }

        /// <summary>
        /// inheritdoc
        /// </summary>
        protected DbContext(DbAccess dbAccess = DbAccess.Automatic)
        {
            this.dbAccess = dbAccess;

            if (dbAccess == DbAccess.Read)
            {
                connectionConfig = GetDbSlaveConfig() ?? throw new DException("未找到数据库链接!");
            }
            else
            {
                connectionConfig = GetDbMasterConfig() ?? throw new DException("未找到数据库链接!");
                connectionSlaveConfig = GetDbSlaveConfig() ?? throw new DException("未找到数据库链接!");
            }

            tablesEngine = DbTables.GetOrAdd(GetType(), type =>
            {
                var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var propertyInfos = type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                var paramterExp = Parameter(typeof(DbContext), "context");

                var variableExp = Variable(type, "variable");

                var setGenericFn = type.GetMethod(nameof(Set), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                var expressions = new List<Expression>(propertyInfos.Length)
                {
                    Assign(variableExp, Convert(paramterExp, type))
                };

                foreach (var propertyInfo in propertyInfos.Where(x => x.PropertyType.IsGenericType && typeof(DbSet<>).IsAssignableFrom(x.PropertyType.GetGenericTypeDefinition())))
                {
                    var tableType = propertyInfo.PropertyType.GetGenericArguments()[0];

                    var tableInfo = TableRegions.Resolve(tableType);

                    string name = tableInfo.TableName;

                    int indexOf = name.IndexOf('.');

                    if (indexOf > 0)
                    {
                        name.Contains(name.Substring(indexOf + 1));
                    }

                    tables.Add(name);

                    if (!propertyInfo.CanWrite)
                    {
                        continue;
                    }

                    var setFn = setGenericFn.MakeGenericMethod(tableType);

                    if (setFn.ReturnType == propertyInfo.PropertyType)
                    {
                        expressions.Add(Assign(Property(variableExp, propertyInfo), Call(variableExp, setFn)));
                    }
                    else
                    {
                        expressions.Add(Assign(Property(variableExp, propertyInfo), Convert(Call(variableExp, setFn), propertyInfo.PropertyType)));
                    }
                }

                var lambdaEx = Lambda<Action<DbContext>>(Block(new ParameterExpression[1] { variableExp }, expressions), paramterExp);

                return new DbTablesEngine(lambdaEx.Compile(), tables);

            }).Initialize(this);

            initialized = true;
        }

        /// <summary>
        /// 获取数据操作器。
        /// </summary>
        /// <typeparam name="TEntity">实体。</typeparam>
        /// <returns></returns>
        public virtual DbSet<TEntity> Set<TEntity>() where TEntity : class, IEntiy
        {
            if (initialized)
            {
                var tableInfo = TableRegions.Resolve<TEntity>();

                string name = tableInfo.TableName;

                int indexOf = name.IndexOf('.');

                if (indexOf > 0)
                {
                    name.Contains(name.Substring(indexOf + 1));
                }

                if (!tablesEngine.IsValid(name))
                {
                    throw new InvalidOperationException();
                }
            }

            return new DbSet<TEntity>(DbAdapter, connectionSlaveConfig.ConnectionString);
        }

        /// <summary>
        /// 获取数据库主库配置。
        /// </summary>
        /// <returns></returns>
        protected virtual IReadOnlyConnectionConfig GetDbMasterConfig()
        {
            var attr = DbMasterConfigCache.GetOrAdd(GetType(), Aw_GetDbMasterConfig);

            return attr?.GetConfig() ?? throw new DException("未找到数据库链接!");
        }

        /// <summary>
        /// 获取数据库从库配置。
        /// </summary>
        /// <returns></returns>
        protected virtual IReadOnlyConnectionConfig GetDbSlaveConfig()
        {
            var attr = DbSlaveConfigCache.GetOrAdd(GetType(), Aw_GetDbConfig);

            if (attr is null)
            {
                return GetDbMasterConfig();
            }

            return attr.GetConfig() ?? throw new DException("未找到数据库链接!");
        }

        private static DbConfigAttribute Aw_GetDbMasterConfig(Type type)
        {
            var attributes = Attribute.GetCustomAttributes(type, typeof(DbConfigAttribute));

            foreach (var attribute in attributes)
            {
                if (attribute is DbWriteConfigAttribute writeConfigAttribute)
                {
                    return writeConfigAttribute;
                }
            }

            foreach (var attribute in attributes)
            {
                if (attribute is DbReadConfigAttribute)
                {
                    continue;
                }

                return (DbConfigAttribute)attribute;
            }

            return null;
        }

        private static DbConfigAttribute Aw_GetDbConfig(Type type)
        {
            var attributes = Attribute.GetCustomAttributes(type, typeof(DbConfigAttribute));

            foreach (var attribute in attributes)
            {
                if (attribute is DbReadConfigAttribute readConfigAttribute)
                {
                    return readConfigAttribute;
                }
            }

            foreach (var attribute in attributes)
            {
                if (attribute is DbWriteConfigAttribute)
                {
                    continue;
                }

                return (DbConfigAttribute)attribute;
            }

            return null;
        }

        /// <summary> 连接名称。 </summary>
        public string Name => connectionConfig.Name;

        /// <summary> 数据库驱动名称。 </summary>
        public string ProviderName => connectionConfig.ProviderName;

        private IDbConnectionAdapter connectionAdapter;
        private bool disposedValue;

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
        protected virtual IDbConnectionAdapter CreateDbAdapter(string providerName) => DapperConnectionManager.Get(providerName);

        /// <summary>
        /// 创建数据库查询器。
        /// </summary>
        /// <param name="useCache">优先复用链接池，否则：始终创建新链接。</param>
        /// <returns></returns>
        protected virtual IDbConnection CreateDb(bool useCache = true) => TransactionConnections.GetConnection(connectionConfig.ConnectionString, DbAdapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, DbAdapter, useCache);

        /// <summary>
        /// 转分页SQL（listSql;countSql;）。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <param name="pageIndex">页面索引。</param>
        /// <param name="pageSize">分页条数。</param>
        /// <returns></returns>
        protected virtual string ToPagedSql(SQL sql, int pageIndex, int pageSize)
        {
            string mainSql = Format(sql.ToSQL(pageIndex, pageSize));

            if (DbAdapter.Settings.Engine == DatabaseEngine.MySQL)
            {
                int i = 6;/* 跳过 SELECT 的计算 */

                for (int length = mainSql.Length; i < length; i++)
                {
                    char c = mainSql[i];

                    if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z'))
                    {
                        break;
                    }
                }

                var sb = new StringBuilder();

                return sb.Append(mainSql, 0, i)
                     .Append(' ')
                     .Append("SQL_CALC_FOUND_ROWS")
                     .Append(mainSql, i, mainSql.Length - i)
                     .Append(';')
                     .Append("SELECT FOUND_ROWS()")
                     .ToString();
            }

            string countSql = Format(sql.ToCountSQL());

            return string.Concat(mainSql, ";", countSql);
        }

        /// <summary>
        /// 将SQL转化为当前数据库规范的语句。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <returns></returns>
        protected virtual string Format(SQL sql)
        {
            if (sql is null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            return sql.ToString(DbAdapter.Settings);
        }

        private bool IsValid(SQL sql)
        {
            for (int i = 0; i < sql.Tables.Count; i++)
            {
                TableToken token = sql.Tables[i];

                if (tablesEngine.IsValid(token.Name))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// 有效读。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <returns></returns>
        protected virtual bool IsReadValid(SQL sql)
            => sql.Tables.All(x => x.CommandType == CommandTypes.Select);

        /// <summary>
        /// 有效写。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <returns></returns>
        protected virtual bool IsWriteValid(SQL sql)
            => sql.Tables.Count > 0 && !sql.Tables.All(x => x.CommandType == CommandTypes.Select);

        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public T QuerySingle<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (!IsValid(sql))
            {
                throw new InvalidOperationException(TransgressionError);
            }

            if (!IsReadValid(sql))
            {
                throw new InvalidOperationException(IllegalReadError);
            }

            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return connection.QuerySingle<T>(sqlStr, param, null, commandTimeout);
            }
        }

        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public T QuerySingleOrDefault<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (!IsValid(sql))
            {
                throw new InvalidOperationException(TransgressionError);
            }

            if (!IsReadValid(sql))
            {
                throw new InvalidOperationException(IllegalReadError);
            }

            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return connection.QuerySingleOrDefault<T>(sqlStr, param, null, commandTimeout);
            }
        }

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public T QueryFirst<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (!IsValid(sql))
            {
                throw new InvalidOperationException(TransgressionError);
            }

            if (!IsReadValid(sql))
            {
                throw new InvalidOperationException(IllegalReadError);
            }

            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return connection.QueryFirst<T>(sqlStr, param, null, commandTimeout);
            }
        }

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public T QueryFirstOrDefault<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (!IsValid(sql))
            {
                throw new InvalidOperationException(TransgressionError);
            }

            if (!IsReadValid(sql))
            {
                throw new InvalidOperationException(IllegalReadError);
            }

            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return connection.QueryFirstOrDefault<T>(sqlStr, param, null, commandTimeout);
            }
        }

        /// <summary>
        /// 查询分页数据。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="pageIndex">页面，索引从“0”开始。</param>
        /// <param name="pageSize">一页显示多少条。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public PagedList<T> Query<T>(SQL sql, int pageIndex, int pageSize, object param = null, int? commandTimeout = null)
        {
            if (!IsValid(sql))
            {
                throw new InvalidOperationException(TransgressionError);
            }

            if (!IsReadValid(sql))
            {
                throw new InvalidOperationException(IllegalReadError);
            }

            var sqlStr = ToPagedSql(sql, pageIndex, pageSize);

            using (IDbConnection connection = CreateDb())
            {
                using (var reader = connection.QueryMultiple(sqlStr, param, commandTimeout: commandTimeout))
                {
                    var results = reader.Read<T>();

                    int count = reader.ReadSingle<int>();

                    return new PagedList<T>(results as List<T> ?? results.ToList(), pageIndex, pageSize, count);
                }
            }
        }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER

        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<T> QuerySingleAsync<T>(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            if (!IsValid(sql))
            {
                throw new InvalidOperationException(TransgressionError);
            }

            if (!IsReadValid(sql))
            {
                throw new InvalidOperationException(IllegalReadError);
            }

            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return await connection.QuerySingleAsync<T>(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
            }
        }

        /// <summary>
        /// 查询唯一的数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<T> QuerySingleOrDefaultAsync<T>(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            if (!IsValid(sql))
            {
                throw new InvalidOperationException(TransgressionError);
            }

            if (!IsReadValid(sql))
            {
                throw new InvalidOperationException(IllegalReadError);
            }

            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return await connection.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
            }
        }

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<T> QueryFirstAsync<T>(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            if (!IsValid(sql))
            {
                throw new InvalidOperationException(TransgressionError);
            }

            if (!IsReadValid(sql))
            {
                throw new InvalidOperationException(IllegalReadError);
            }

            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return await connection.QueryFirstAsync<T>(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
            }
        }

        /// <summary>
        /// 查询第一条数据。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<T> QueryFirstOrDefaultAsync<T>(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return await connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
            }
        }

        /// <summary>
        /// 查询分页数据。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="pageIndex">页面，索引从“0”开始。</param>
        /// <param name="pageSize">一页显示多少条。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<PagedList<T>> QueryAsync<T>(SQL sql, int pageIndex, int pageSize, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var sqlStr = ToPagedSql(sql, pageIndex, pageSize);

            using (IDbConnection connection = CreateDb())
            {
                using (var reader = await connection.QueryMultipleAsync(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken)).ConfigureAwait(false))
                {
                    var results = await reader.ReadAsync<T>().ConfigureAwait(false);

                    int count = await reader.ReadSingleAsync<int>().ConfigureAwait(false);

                    return new PagedList<T>(results as List<T> ?? results.ToList(), pageIndex, pageSize, count);
                }
            }
        }
#endif

        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public int Execute(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (dbAccess == DbAccess.Read)
            {
                throw new InvalidOperationException();
            }

            if (!IsValid(sql))
            {
                throw new InvalidOperationException(TransgressionError);
            }

            if (!IsWriteValid(sql))
            {
                throw new InvalidOperationException(IllegalWriteError);
            }

            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return connection.Execute(sqlStr, param, null, commandTimeout);
            }
        }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消指令。</param>
        /// <returns></returns>
        public async Task<int> ExecuteAsync(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            if (dbAccess == DbAccess.Read)
            {
                throw new InvalidOperationException();
            }

            if (!IsValid(sql))
            {
                throw new InvalidOperationException(TransgressionError);
            }

            if (!IsWriteValid(sql))
            {
                throw new InvalidOperationException(IllegalWriteError);
            }

            var sqlStr = Format(sql);

            using (IDbConnection connection = CreateDb())
            {
                return await connection.ExecuteAsync(new CommandDefinition(sqlStr, param, null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
            }
        }
#endif

        /// <summary>
        /// 释放。
        /// </summary>
        /// <param name="disposing">释放。</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        /// ~DbContext()
        /// {
        ///     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        ///     Dispose(disposing: false);
        /// }
        /// </summary>
        public void Dispose()
        {
            if (!disposedValue)
            {
                disposedValue = true;

                // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
                Dispose(disposing: true);
            }
            else
            {
                Dispose(false);
            }

            GC.SuppressFinalize(this);
        }
    }
}

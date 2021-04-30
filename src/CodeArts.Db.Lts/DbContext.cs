using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据上下文。
    /// </summary>
    public class DbContext : IDbContext
    {
        private sealed class NestedDbCommand : DbCommand
        {
            public NestedDbCommand(DbCommand command) : base(command)
            {
            }

            public override DbTransaction Transaction { get => base.Transaction; set => throw new NotSupportedException(); }
        }
        private sealed class DbCommandFactory : IDbCommandFactory
        {
            private readonly DbConnection connection;
            private readonly DbTransaction transaction;

            public DbCommandFactory(DbConnection connection, DbTransaction transaction)
            {
                this.connection = connection;
                this.transaction = transaction;
            }

            public DbCommand CreateCommand()
            {
                DbCommand command = connection.CreateCommand();

                command.Transaction = transaction;

                return new NestedDbCommand(command);
            }
        }
        private class DbAssistant<T> where T : class, IEntiy
        {
            static readonly ITableInfo regions;
            static DbAssistant() => regions = TableRegions.Resolve<T>();
            public static bool IsEquals(string value) => string.Equals(regions.TableName, value, StringComparison.OrdinalIgnoreCase);
        }

        private readonly IReadOnlyConnectionConfig connectionConfig;
        
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connectionConfig">数据连接配置。</param>
        public DbContext(IReadOnlyConnectionConfig connectionConfig)
        {
            this.connectionConfig = connectionConfig ?? throw new ArgumentNullException(nameof(connectionConfig));
        }

        /// <summary> 连接名称。 </summary>
        public string Name => connectionConfig.Name;

        /// <summary> 数据库驱动名称。 </summary>
        public string ProviderName => connectionConfig.ProviderName;

        /// <summary>
        /// SQL 矫正。
        /// </summary>
        public ISQLCorrectSettings Settings => DbAdapter.Settings;


        private IDbConnectionLtsAdapter connectionAdapter;

        /// <summary>
        /// 适配器。
        /// </summary>
        protected IDbConnectionLtsAdapter DbAdapter
        {
            get
            {
                if (connectionAdapter is null || !string.Equals(connectionAdapter.ProviderName, connectionConfig.ProviderName))
                {
                    repositoryProvider = null;

                    connectionAdapter = CreateDbAdapter(connectionConfig.ProviderName);
                }

                return connectionAdapter;
            }
        }

        /// <summary>
        /// 创建适配器。
        /// </summary>
        /// <returns></returns>
        protected virtual IDbConnectionLtsAdapter CreateDbAdapter(string providerName) => DbConnectionManager.Get(providerName);

        private IDbRepositoryProvider repositoryProvider;

        /// <summary>
        /// 数据供应器。
        /// </summary>
#if NETSTANDARD2_1
        protected IDbRepositoryProvider DbProvider => repositoryProvider ??= CreateDbProvider();
#else
        protected IDbRepositoryProvider DbProvider => repositoryProvider ?? (repositoryProvider = CreateDbProvider());
#endif

        /// <summary>
        /// 创建查询器。
        /// </summary>
        /// <returns></returns>
        protected virtual IDbRepositoryProvider CreateDbProvider() => DbConnectionManager.Create(DbAdapter);

        private IDbRepositoryExecuter repositoryExecuter;

        /// <summary>
        /// 执行器。
        /// </summary>
#if NETSTANDARD2_1
        protected IDbRepositoryExecuter DbExecuter => repositoryExecuter ??= CreateDbExecuter();
#else
        protected IDbRepositoryExecuter DbExecuter => repositoryExecuter ?? (repositoryExecuter = CreateDbExecuter());
#endif

        /// <summary>
        /// 创建执行器。
        /// </summary>
        /// <returns></returns>
        protected virtual IDbRepositoryExecuter CreateDbExecuter() => DbConnectionManager.Create(DbAdapter);

        /// <summary>
        /// 创建数据库链接。
        /// </summary>
        /// <returns></returns>
        DbConnection IDbContext.CreateDb() => PriviteCreateDb(true);

        private DbConnection PriviteCreateDb(bool useCache) => new DbConnection(CreateDb(useCache), Settings);

        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <typeparam name="T">返回结果。</typeparam>
        /// <param name="inTransactionExecution">事务中执行。</param>
        /// <returns></returns>
        public T Transaction<T>(Func<IDbCommandFactory, T> inTransactionExecution)
        {
            if (inTransactionExecution is null)
            {
                throw new ArgumentNullException(nameof(inTransactionExecution));
            }

            using (var connection = PriviteCreateDb(false))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    var commandFactory = new DbCommandFactory(connection, transaction);

                    try
                    {
                        var result = inTransactionExecution.Invoke(commandFactory);

                        transaction.Commit();

                        return result;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();

                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <typeparam name="T">返回结果。</typeparam>
        /// <param name="inTransactionExecution">事务中执行。</param>
        /// <param name="isolationLevel">事务隔离级别。</param>
        /// <returns></returns>
        public T Transaction<T>(Func<IDbCommandFactory, T> inTransactionExecution, System.Data.IsolationLevel isolationLevel)
        {
            if (inTransactionExecution is null)
            {
                throw new ArgumentNullException(nameof(inTransactionExecution));
            }

            using (var connection = PriviteCreateDb(false))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(isolationLevel))
                {
                    var commandFactory = new DbCommandFactory(connection, transaction);

                    try
                    {
                        var result = inTransactionExecution.Invoke(commandFactory);

                        transaction.Commit();

                        return result;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();

                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 创建数据库查询器。
        /// </summary>
        /// <param name="useCache">优先复用链接池，否则：始终创建新链接。</param>
        /// <returns></returns>
        protected virtual IDbConnection CreateDb(bool useCache = true) => TransactionConnections.GetConnection(connectionConfig.ConnectionString, DbAdapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, DbAdapter, useCache);

        /// <summary>
        /// 读取命令。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        protected virtual CommandSql<T> CreateReadCommandSql<T>(Expression expression)
        {
            using (var visitor = DbProvider.Create())
            {
                T defaultValue = default;

                visitor.Startup(expression);

                if (visitor.HasDefaultValue)
                {
                    if (visitor.DefaultValue is T value)
                    {
                        defaultValue = value;
                    }
                    else if (!(visitor.DefaultValue is null))
                    {
                        throw new DSyntaxErrorException($"查询结果类型({typeof(T)})和指定的默认值类型({visitor.DefaultValue.GetType()})无法进行默认转换!");
                    }
                }

                string sql = visitor.ToSQL();

                if (visitor.Required)
                {
                    return new CommandSql<T>(sql, visitor.Parameters, visitor.TimeOut, visitor.HasDefaultValue, defaultValue, visitor.MissingDataError);
                }

                return new CommandSql<T>(sql, visitor.Parameters, visitor.TimeOut, defaultValue);
            }
        }

        /// <summary>
        /// 验证SQL可读性。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <returns></returns>
        protected virtual bool AuthorizeRead(SQL sql)
        {
            foreach (var token in sql.Tables)
            {
                if (token.CommandType == CommandTypes.Select)
                {
                    continue;
                }

                throw new NonAuthorizedException($"当前语句对表“{token.Name}”存在非查询操作！");
            }

            return true;
        }

        /// <summary>
        /// 参数准备。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <returns></returns>
        protected virtual object PreparationParameters(SQL sql, object param)
        {
            if (param is null)
            {
                return null;
            }

            var paramType = param.GetType();

            if (paramType.IsValueType || paramType == typeof(string))
            {
                if (sql.Parameters.Count > 1)
                {
                    throw new NotSupportedException();
                }

                return new Dictionary<string, object>
                {
                    [sql.Parameters.First()] = param
                };
            }

            return param;
        }

        /// <summary>
        /// 读取首条数据。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public T Read<T>(Expression expression)
        {
            var commandSql = CreateReadCommandSql<T>(expression);

            SqlCapture.Current?.Capture(commandSql);

            return DbProvider.Read(this, commandSql);
        }

        /// <summary>
        /// 读取数据集合。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(Expression expression)
        {
            var commandSql = CreateReadCommandSql<T>(expression);

            SqlCapture.Current?.Capture(commandSql);

            return DbProvider.Query<T>(this, commandSql);
        }

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="hasDefaultValue">是否包含默认值。</param>
        /// <param name="defaultValue">默认值（仅“<paramref name="hasDefaultValue"/>”为真时，有效）。</param>
        /// <param name="missingMsg">未查询到数据时，异常信息。</param>
        /// <returns></returns>
        public T Read<T>(SQL sql, object param = null, int? commandTimeout = null, bool hasDefaultValue = true, T defaultValue = default, string missingMsg = null)
        {
            var commandSql = new CommandSql<T>(sql.ToString(Settings), PreparationParameters(sql, param), commandTimeout, hasDefaultValue, defaultValue, missingMsg);

            SqlCapture.Current?.Capture(commandSql);

            return DbProvider.Read(this, commandSql);
        }

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            var commandSql = new CommandSql(sql.ToString(Settings), PreparationParameters(sql, param), commandTimeout);

            SqlCapture.Current?.Capture(commandSql);

            return DbProvider.Query<T>(this, commandSql);
        }

#if NET_NORMAL || NET_CORE

        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <typeparam name="T">返回结果。</typeparam>
        /// <param name="inTransactionExecution">事务中执行。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public async Task<T> TransactionAsync<T>(Func<IDbCommandFactory, Task<T>> inTransactionExecution, CancellationToken cancellationToken = default)
        {
            if (inTransactionExecution is null)
            {
                throw new ArgumentNullException(nameof(inTransactionExecution));
            }

            using (var connection = PriviteCreateDb(false))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

#if NETSTANDARD2_1
                await using (var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
#else
                using (var transaction = connection.BeginTransaction())
#endif
                {
                    var commandFactory = new DbCommandFactory(connection, transaction);

                    try
                    {
                        var result = await inTransactionExecution.Invoke(commandFactory);

#if NETSTANDARD2_1
                        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
#else
                        transaction.Commit();
#endif

                        return result;
                    }
                    catch (Exception)
                    {
#if NETSTANDARD2_1
                        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
#else
                        transaction.Rollback();
#endif

                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <typeparam name="T">返回结果。</typeparam>
        /// <param name="inTransactionExecution">事务中执行。</param>
        /// <param name="isolationLevel">事务隔离级别。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public async Task<T> TransactionAsync<T>(Func<IDbCommandFactory, Task<T>> inTransactionExecution, System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        {
            if (inTransactionExecution is null)
            {
                throw new ArgumentNullException(nameof(inTransactionExecution));
            }

            using (var connection = PriviteCreateDb(false))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

#if NETSTANDARD2_1
                await using (var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false))
#else
                using (var transaction = connection.BeginTransaction(isolationLevel))
#endif
                {
                    var commandFactory = new DbCommandFactory(connection, transaction);

                    try
                    {
                        var result = await inTransactionExecution.Invoke(commandFactory);

#if NETSTANDARD2_1
                        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
#else
                        transaction.Commit();
#endif

                        return result;
                    }
                    catch (Exception)
                    {
#if NETSTANDARD2_1
                        await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
#else
                        transaction.Rollback();
#endif

                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<T> ReadAsync<T>(Expression expression, CancellationToken cancellationToken = default)
        {
            var commandSql = CreateReadCommandSql<T>(expression);

            SqlCapture.Current?.Capture(commandSql);

            return DbProvider.ReadAsync(this, commandSql, cancellationToken);
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public IAsyncEnumerable<T> QueryAsync<T>(Expression expression)
        {
            var commandSql = CreateReadCommandSql<T>(expression);

            SqlCapture.Current?.Capture(commandSql);

            return DbProvider.QueryAsync<T>(this, commandSql);
        }

        /// <summary>
        /// 查询第一个结果。
        /// </summary>
        /// <typeparam name="T">结果类型。</typeparam>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="hasDefaultValue">是否包含默认值。</param>
        /// <param name="defaultValue">默认值（仅“<paramref name="hasDefaultValue"/>”为真时，有效）。</param>
        /// <param name="missingMsg">未查询到数据时，异常信息。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<T> ReadAsync<T>(SQL sql, object param = null, int? commandTimeout = null, bool hasDefaultValue = true, T defaultValue = default, string missingMsg = null, CancellationToken cancellationToken = default)
        {
            if (!AuthorizeRead(sql))
            {
                throw new NonAuthorizedException();
            }

            var commandSql = new CommandSql<T>(sql.ToString(Settings), PreparationParameters(sql, param), commandTimeout, hasDefaultValue, defaultValue, missingMsg);

            SqlCapture.Current?.Capture(commandSql);

            return DbProvider.ReadAsync(this, commandSql, cancellationToken);
        }

        /// <summary>
        /// 查询列表集合。
        /// </summary>
        /// <typeparam name="T">集合元素类型。</typeparam>
        /// <param name="sql">查询语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public IAsyncEnumerable<T> QueryAsync<T>(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (!AuthorizeRead(sql))
            {
                throw new NonAuthorizedException();
            }

            var commandSql = new CommandSql(sql.ToString(Settings), PreparationParameters(sql, param), commandTimeout);

            SqlCapture.Current?.Capture(commandSql);

            return DbProvider.QueryAsync<T>(this, commandSql);
        }
#endif

        /// <summary>
        /// 验证SQL可写性。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <returns></returns>
        protected virtual bool AuthorizeWrite<T>(SQL sql) where T : class, IEntiy
        {
            bool flag = false;

            foreach (var token in sql.Tables)
            {
                if (token.CommandType == CommandTypes.Select)
                {
                    continue;
                }

                if (DbAssistant<T>.IsEquals(token.Name))
                {
                    flag = true;

                    continue;
                }

                throw new NonAuthorizedException($"禁止在当前数据库上下文对“{token.Name}”表做非查询操作！");
            }

            return flag;
        }

        /// <summary>
        /// 写入命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        protected virtual CommandSql CreateWriteCommandSql(Expression expression)
        {
            using (var visitor = DbExecuter.CreateExe())
            {
                visitor.Startup(expression);

                string sql = visitor.ToSQL();

                return new CommandSql(sql, visitor.Parameters, visitor.TimeOut);
            }
        }

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public int Execute(Expression expression)
        {
            var commandSql = CreateWriteCommandSql(expression);

            SqlCapture.Current?.Capture(commandSql);

            return DbExecuter.Execute(this, commandSql);
        }

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns>执行影响行。</returns>
        public int Execute(SQL sql, object param = null, int? commandTimeout = null)
        {
            var commandSql = new CommandSql(sql.ToString(Settings), PreparationParameters(sql, param), commandTimeout);

            SqlCapture.Current?.Capture(commandSql);

            return DbExecuter.Execute(this, commandSql);
        }

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns>执行影响行。</returns>
        public int Execute<T>(SQL sql, object param = null, int? commandTimeout = null) where T : class, IEntiy
        {
            if (!AuthorizeWrite<T>(sql))
            {
                throw new NonAuthorizedException();
            }

            return Execute(sql, param, commandTimeout);
        }

#if NET_NORMAL || NET_CORE
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
        {
            var commandSql = CreateWriteCommandSql(expression);

            SqlCapture.Current?.Capture(commandSql);

            return DbExecuter.ExecuteAsync(this, commandSql, cancellationToken);
        }

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>执行影响行。</returns>
        public virtual Task<int> ExecuteAsync(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var commandSql = new CommandSql(sql.ToString(Settings), PreparationParameters(sql, param), commandTimeout);

            SqlCapture.Current?.Capture(commandSql);

            return DbExecuter.ExecuteAsync(this, commandSql, cancellationToken);
        }

        /// <summary>
        /// 执行增删改功能。
        /// </summary>
        /// <param name="sql">执行语句。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns>执行影响行。</returns>
        public Task<int> ExecuteAsync<T>(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where T : class, IEntiy
        {
            if (!AuthorizeWrite<T>(sql))
            {
                throw new NonAuthorizedException();
            }

            return ExecuteAsync(sql, param, commandTimeout, cancellationToken);
        }

#endif
    }
}

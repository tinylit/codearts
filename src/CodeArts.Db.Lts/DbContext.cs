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

        /// <summary>
        /// 创建数据库链接。
        /// </summary>
        /// <returns></returns>
        DbConnection IDbContext.CreateDb() => new DbConnection(CreateDb(), Settings);

        /// <summary>
        /// 创建数据库查询器。
        /// </summary>
        /// <returns></returns>
        protected virtual IDbConnection CreateDb() => TransactionConnections.GetConnection(connectionConfig.ConnectionString, DbAdapter) ?? DispatchConnections.Instance.GetConnection(connectionConfig.ConnectionString, DbAdapter);

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

                var token = sql.Parameters.First();

                return new Dictionary<string, object>
                {
                    [token.Name] = param
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
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<T> ReadAsync<T>(Expression expression, CancellationToken cancellationToken = default)
        {
            var commandSql = CreateReadCommandSql<T>(expression);

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

            return DbProvider.QueryAsync<T>(this, commandSql);
        }

#endif
    }

    /// <summary>
    /// 数据上下文。
    /// </summary>
    public class DbContext<TEntity> : DbContext, IDbContext<TEntity>, IDbContext where TEntity : class, IEntiy
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connectionConfig">数据连接配置。</param>
        public DbContext(IReadOnlyConnectionConfig connectionConfig) : base(connectionConfig)
        {

        }


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
        /// 创建适配器。
        /// </summary>
        /// <returns></returns>
        protected override IDbConnectionLtsAdapter CreateDbAdapter(string providerName)
        {
            repositoryExecuter = null;

            return base.CreateDbAdapter(providerName);
        }

        /// <summary>
        /// 创建执行器。
        /// </summary>
        /// <returns></returns>
        protected virtual IDbRepositoryExecuter CreateDbExecuter() => DbConnectionManager.Create(DbAdapter);

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
        /// 验证SQL可读性。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <returns></returns>
        protected override bool AuthorizeRead(SQL sql)
        {
            string name = DbWriter<TEntity>.TableInfo.TableName;

            if (!sql.Tables.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new NonAuthorizedException($"在当前数据库上下文未对“{name}”表做任何操作！");
            }

            return base.AuthorizeRead(sql);
        }

        /// <summary>
        /// 验证SQL可写性。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <returns></returns>
        protected virtual bool AuthorizeWrite(SQL sql)
        {
            bool flag = false;

            string name = DbWriter<TEntity>.TableInfo.TableName;

            foreach (var token in sql.Tables)
            {
                if (token.CommandType == CommandTypes.Select)
                {
                    continue;
                }

                if (string.Equals(token.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    flag = true;

                    continue;
                }

                throw new NonAuthorizedException($"禁止在当前数据库上下文对“{token.Name}”表做非查询操作！");
            }

            return flag;
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
            if (!AuthorizeWrite(sql))
            {
                throw new NonAuthorizedException();
            }

            var commandSql = new CommandSql(sql.ToString(Settings), PreparationParameters(sql, param), commandTimeout);

            SqlCapture.Current?.Capture(commandSql);

            return DbExecuter.Execute(this, commandSql);
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
        public Task<int> ExecuteAsync(SQL sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            if (!AuthorizeWrite(sql))
            {
                throw new NonAuthorizedException();
            }

            var commandSql = new CommandSql(sql.ToString(Settings), PreparationParameters(sql, param), commandTimeout);

            return DbExecuter.ExecuteAsync(this, commandSql, cancellationToken);
        }

#endif
    }
}

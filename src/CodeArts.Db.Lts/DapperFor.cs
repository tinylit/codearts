using CodeArts.Db.Exceptions;
using Dapper;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// Dapper for Lts。
    /// </summary>
    public class DapperFor : DatabaseFor, IDatabaseFor
    {
        private readonly IDbConnectionLtsAdapter adapter;

        private class BooleanHandler : SqlMapper.ITypeHandler
        {
            public object Parse(Type destinationType, object value) => Convert.ChangeType(value, typeof(bool));

            public void SetValue(IDbDataParameter parameter, object value)
            {
                parameter.Value = Convert.ChangeType(value, typeof(bool));
                parameter.DbType = DbType.Boolean;
            }
        }

        private sealed class DoubleHandler : SqlMapper.ITypeHandler
        {
            public object Parse(Type destinationType, object value) => Convert.ChangeType(value, typeof(double));

            public void SetValue(IDbDataParameter parameter, object value)
            {
                parameter.Value = Convert.ChangeType(value, typeof(double));
                parameter.DbType = DbType.Double;
            }
        }

        private sealed class DoubleNullableHandler : SqlMapper.ITypeHandler
        {
            public object Parse(Type destinationType, object value)
            {
                if (value is null)
                {
                    return null;
                }

                return Convert.ChangeType(value, typeof(double));
            }

            public void SetValue(IDbDataParameter parameter, object value)
            {
                if (value is null)
                {
                    parameter.Value = DBNull.Value;
                }
                else
                {
                    parameter.Value = Convert.ChangeType(value, typeof(double));
                }

                parameter.DbType = DbType.Double;
            }
        }

        private static object FixParameters(object parameters)
        {
            switch (parameters)
            {
                case IEnumerable<KeyValuePair<string, ParameterValue>> parameterValues:
                    {
                        var results = new DynamicParameters();

                        foreach (var kv in parameterValues)
                        {
                            if (kv.Value.IsNull)
                            {
                                results.Add(kv.Key, DBNull.Value, LookupDb.For(kv.Value.ValueType));
                            }
                            else
                            {
                                results.Add(kv.Key, kv.Value.Value);
                            }
                        }
                        return results;
                    }
                case IEnumerable<KeyValuePair<string, object>> keyValuePairs when keyValuePairs.Any(x => x.Value is ParameterValue):
                    {
                        var results = new DynamicParameters();

                        foreach (var kv in keyValuePairs)
                        {
                            if (kv.Value is ParameterValue parameterValue)
                            {
                                if (parameterValue.IsNull)
                                {
                                    results.Add(kv.Key, DBNull.Value, LookupDb.For(parameterValue.ValueType));
                                }
                                else
                                {
                                    results.Add(kv.Key, parameterValue.Value);
                                }
                            }
                            else
                            {
                                results.Add(kv.Key, kv.Value);
                            }
                        }

                        return results;
                    }
                default:
                    return parameters;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public DapperFor(IDbConnectionLtsAdapter adapter) : base(adapter.Settings, adapter.Visitors)
        {
            this.adapter = adapter;
        }

        /// <summary>
        /// 创建数据库查询器。
        /// </summary>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="useCache">优先复用链接池，否则：始终创建新链接。</param>
        /// <returns></returns>
        protected virtual IDbConnection CreateDb(string connectionString, bool useCache = true) => TransactionConnections.GetConnection(connectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionString, adapter, useCache);

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public int Execute(IDbConnection connection, CommandSql commandSql)
        => connection.Execute(commandSql.Sql, FixParameters(commandSql.Parameters), null, commandSql.CommandTimeout);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> ExecuteAsync(IDbConnection connection, CommandSql commandSql, CancellationToken cancellationToken)
        => connection.ExecuteAsync(commandSql.Sql, FixParameters(commandSql.Parameters), null, commandSql.CommandTimeout);
#endif
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(IDbConnection connection, CommandSql commandSql)
        => connection.Query<T>(commandSql.Sql, FixParameters(commandSql.Parameters), null, true, commandSql.CommandTimeout);


#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connectionString">连接字符串。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public IAsyncEnumerable<T> QueryAsync<T>(string connectionString, CommandSql commandSql)
        => new AsyncEnumerable<T>(connectionString, adapter, commandSql);

        private sealed class AsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly string connectionString;
            private readonly IDbConnectionLtsAdapter adapter;
            private readonly CommandSql commandSql;

            private IAsyncEnumerator<T> enumerator;

            public AsyncEnumerable(string connectionString, IDbConnectionLtsAdapter adapter, CommandSql commandSql)
            {
                this.connectionString = connectionString;
                this.adapter = adapter;
                this.commandSql = commandSql;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
#if NETSTANDARD2_1_OR_GREATER
                => enumerator ??= new AsyncEnumerator<T>(connectionString, adapter, commandSql, cancellationToken);
#else
                => enumerator ?? (enumerator = new AsyncEnumerator<T>(connectionString, adapter, commandSql, cancellationToken));
#endif
        }

        private sealed class AsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private IEnumerator<T> enumerator;

            private readonly string connectionString;
            private readonly IDbConnectionLtsAdapter adapter;
            private readonly CommandSql commandSql;

            private readonly CancellationToken cancellationToken;

            public AsyncEnumerator(string connectionString, IDbConnectionLtsAdapter adapter, CommandSql commandSql, CancellationToken cancellationToken)
            {
                this.connectionString = connectionString;
                this.adapter = adapter;
                this.commandSql = commandSql;
                this.cancellationToken = cancellationToken;
            }

            public T Current => enumerator.Current;

#if NETSTANDARD2_1_OR_GREATER
            public async ValueTask<bool> MoveNextAsync()
#else
            public async Task<bool> MoveNextAsync()
#endif
            {
                if (enumerator is null)
                {
                    using (var connection = TransactionConnections.GetConnection(connectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionString, adapter, true))
                    {
                        var results = await connection.QueryAsync<T>(new CommandDefinition(commandSql.Sql, FixParameters(commandSql.Parameters), null, commandSql.CommandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));

                        enumerator = results.GetEnumerator();
                    }
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return enumerator.MoveNext();
            }

#if NETSTANDARD2_1_OR_GREATER
            public ValueTask DisposeAsync() => new ValueTask(Task.Run(enumerator.Dispose));
#endif

            public void Dispose() => enumerator.Dispose();
        }
#endif
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public T Read<T>(IDbConnection connection, CommandSql<T> commandSql)
        {
            object value;
            var type = typeof(T);

            var conversionType = type.IsValueType && !type.IsNullable()
                ? typeof(Nullable<>).MakeGenericType(type)
                : type;

            switch (commandSql.RowStyle)
            {
                case RowStyle.None:
                case RowStyle.First:
                case RowStyle.FirstOrDefault:
                    value = connection.QueryFirstOrDefault(conversionType, commandSql.Sql, FixParameters(commandSql.Parameters), null, commandSql.CommandTimeout);
                    break;
                case RowStyle.Single:
                case RowStyle.SingleOrDefault:
                    value = connection.QuerySingleOrDefault(conversionType, commandSql.Sql, FixParameters(commandSql.Parameters), null, commandSql.CommandTimeout);
                    break;
                default:
                    throw new NotSupportedException();
            }

            if (value is null)
            {
                if (commandSql.HasDefaultValue)
                {
                    return commandSql.DefaultValue;
                }

                if ((commandSql.RowStyle & RowStyle.FirstOrDefault) == RowStyle.FirstOrDefault)
                {
                    return default;
                }

                throw new DRequiredException(commandSql.MissingMsg);
            }

            return (T)value;
        }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public async Task<T> ReadAsync<T>(IDbConnection connection, CommandSql<T> commandSql, CancellationToken cancellationToken)
        {
            object value;
            var type = typeof(T);
            var conversionType = type.IsValueType && !type.IsNullable()
                ? typeof(Nullable<>).MakeGenericType(type)
                : type;

            switch (commandSql.RowStyle)
            {
                case RowStyle.None:
                case RowStyle.First:
                case RowStyle.FirstOrDefault:
                    value = await connection.QueryFirstOrDefaultAsync(conversionType, commandSql.Sql, FixParameters(commandSql.Parameters), null, commandSql.CommandTimeout);
                    break;
                case RowStyle.Single:
                case RowStyle.SingleOrDefault:
                    value = await connection.QuerySingleOrDefaultAsync(conversionType, commandSql.Sql, FixParameters(commandSql.Parameters), null, commandSql.CommandTimeout);
                    break;
                default:
                    throw new NotSupportedException();
            }

            if (value is null)
            {
                if (commandSql.HasDefaultValue)
                {
                    return commandSql.DefaultValue;
                }

                if ((commandSql.RowStyle & RowStyle.FirstOrDefault) == RowStyle.FirstOrDefault)
                {
                    return default;
                }

                throw new DRequiredException(commandSql.MissingMsg);
            }

            return (T)value;
        }
#endif


        /// <summary>
        /// 分析读取SQL。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public T Read<T>(string connectionString, Expression expression)
        {
            using (var connection = CreateDb(connectionString))
            {
                return Read<T>(connection, Read<T>(expression));
            }
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回元素类型。</typeparam>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(string connectionString, Expression expression)
        {
            using (var connection = CreateDb(connectionString))
            {
                return Query<T>(connection, Read<T>(expression));
            }
        }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public async Task<T> ReadAsync<T>(string connectionString, Expression expression, CancellationToken cancellationToken = default)
        {
            using (var connection = CreateDb(connectionString))
            {
                return await ReadAsync<T>(connection, Read<T>(expression), cancellationToken);
            }
        }

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回元素类型。</typeparam>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public IAsyncEnumerable<T> QueryAsync<T>(string connectionString, Expression expression) => QueryAsync<T>(connectionString, Read<T>(expression));
#endif

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public int Execute(string connectionString, Expression expression)
        {
            using (var connection = CreateDb(connectionString))
            {
                return Execute(connection, Execute(expression));
            }
        }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connectionString">数据库连接。</param>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public async Task<int> ExecuteAsync(string connectionString, Expression expression, CancellationToken cancellationToken = default)
        {
            using (var connection = CreateDb(connectionString))
            {
                return await ExecuteAsync(connection, Execute(expression), cancellationToken);
            }
        }
#endif
    }
}

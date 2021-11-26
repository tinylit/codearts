using CodeArts.Db.Exceptions;
using Dapper;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// Dapper for Lts。
    /// </summary>
    public class DapperFor : DatabaseFor
    {
        private static readonly Dictionary<Type, DbType> typeMap;

        private static DbType LookupDbType(Type dataType)
        {
            if (dataType.IsNullable())
            {
                dataType = Nullable.GetUnderlyingType(dataType);
            }

            if (dataType.IsEnum)
            {
                dataType = Enum.GetUnderlyingType(dataType);
            }

            if (typeMap.TryGetValue(dataType, out DbType dbType))
            {
                return dbType;
            }

            if (dataType.FullName == "System.Data.Linq.Binary")
            {
                return DbType.Binary;
            }

            return DbType.Object;
        }
        static DapperFor()
        {
            //? linq Average 函数。
            SqlMapper.AddTypeHandler(typeof(double?), new DoubleNullableHandler());
            SqlMapper.AddTypeHandler(typeof(double), new DoubleHandler());
            //? MySQL 动态字段，没有行数据时，bool识别为长整型的问题。
            SqlMapper.AddTypeHandler(typeof(bool), new BooleanHandler());

            typeMap = new Dictionary<Type, DbType>
            {
                [typeof(byte)] = DbType.Byte,
                [typeof(sbyte)] = DbType.SByte,
                [typeof(short)] = DbType.Int16,
                [typeof(ushort)] = DbType.UInt16,
                [typeof(int)] = DbType.Int32,
                [typeof(uint)] = DbType.UInt32,
                [typeof(long)] = DbType.Int64,
                [typeof(ulong)] = DbType.UInt64,
                [typeof(float)] = DbType.Single,
                [typeof(double)] = DbType.Double,
                [typeof(decimal)] = DbType.Decimal,
                [typeof(bool)] = DbType.Boolean,
                [typeof(string)] = DbType.String,
                [typeof(char)] = DbType.StringFixedLength,
                [typeof(Guid)] = DbType.Guid,
                [typeof(DateTime)] = DbType.DateTime,
                [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
                [typeof(TimeSpan)] = DbType.Time,
                [typeof(byte[])] = DbType.Binary,
                [typeof(object)] = DbType.Object
            };
        }

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
                                results.Add(kv.Key, DBNull.Value, LookupDbType(kv.Value.ValueType));
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
                                    results.Add(kv.Key, DBNull.Value, LookupDbType(parameterValue.ValueType));
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
        public DapperFor(ISQLCorrectSettings settings, ICustomVisitorList visitors) : base(settings, visitors)
        {
        }

        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public override int Execute(IDbConnection connection, CommandSql commandSql)
        => connection.Execute(commandSql.Sql, FixParameters(commandSql.Parameters), null, commandSql.CommandTimeout);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public override Task<int> ExecuteAsync(IDbConnection connection, CommandSql commandSql, CancellationToken cancellationToken)
        => connection.ExecuteAsync(commandSql.Sql, FixParameters(commandSql.Parameters), null, commandSql.CommandTimeout);
#endif
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public override IEnumerable<T> Query<T>(IDbConnection connection, CommandSql commandSql)
        => connection.Query<T>(commandSql.Sql, FixParameters(commandSql.Parameters), null, true, commandSql.CommandTimeout);


#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public override IAsyncEnumerable<T> QueryAsync<T>(IDbConnection connection, CommandSql commandSql)
        => new AsyncEnumerable<T>(connection, commandSql);

        private sealed class AsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IDbConnection connection;
            private readonly CommandSql commandSql;

            private IAsyncEnumerator<T> enumerator;

            public AsyncEnumerable(IDbConnection connection, CommandSql commandSql)
            {
                this.connection = connection;
                this.commandSql = commandSql;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
#if NETSTANDARD2_1_OR_GREATER
                => enumerator ??= new AsyncEnumerator<T>(connection, commandSql, cancellationToken);
#else
                => enumerator ?? (enumerator = new AsyncEnumerator<T>(connection, commandSql, cancellationToken));
#endif
        }

        private sealed class AsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private IEnumerator<T> enumerator;

            private readonly IDbConnection connection;
            private readonly CommandSql commandSql;

            private readonly CancellationToken cancellationToken;

            public AsyncEnumerator(IDbConnection connection, CommandSql commandSql, CancellationToken cancellationToken)
            {
                this.connection = connection;
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
                    var results = await connection.QueryAsync<T>(new CommandDefinition(commandSql.Sql, FixParameters(commandSql.Parameters), null, commandSql.CommandTimeout, CommandType.Text, CommandFlags.None, cancellationToken));

                    enumerator = results.GetEnumerator();
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
        public override T Read<T>(IDbConnection connection, CommandSql<T> commandSql)
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
        public override async Task<T> ReadAsync<T>(IDbConnection connection, CommandSql<T> commandSql, CancellationToken cancellationToken)
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
    }
}

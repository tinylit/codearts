using CodeArts.Db.Exceptions;
using CodeArts.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static System.Linq.Expressions.Expression;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// Dapper for Lts。
    /// </summary>
    public class DapperFor : DatabaseFor, IDatabaseFor
    {
        private readonly IDbConnectionLtsAdapter adapter;

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
        {
            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                connection.Open();
            }

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = commandSql.Sql;
                    command.CommandType = CommandType.Text;

                    if (commandSql.CommandTimeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.CommandTimeout.Value;
                    }

                    foreach (var item in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, item.Key, item.Value);
                    }

                    return command.ExecuteNonQuery();
                }
            }
            finally
            {
                if (isClosedConnection)
                {
                    connection.Close();
                }
            }
        }

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public async Task<int> ExecuteAsync(IDbConnection connection, CommandSql commandSql, CancellationToken cancellationToken)
        {
            if (!(connection is DbConnection dbConnection))
            {
                throw new InvalidOperationException("Async operations require use of a DbConnection or an already-open IDbConnection");
            }

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                await dbConnection.OpenAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            try
            {
                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = commandSql.Sql;
                    command.CommandType = CommandType.Text;

                    if (commandSql.CommandTimeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.CommandTimeout.Value;
                    }

                    foreach (var item in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, item.Key, item.Value);
                    }

                    return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (isClosedConnection)
                {
#if NETSTANDARD2_1_OR_GREATER
                    await dbConnection.CloseAsync().ConfigureAwait(false);
#else
                    dbConnection.Close();
#endif
                }
            }
        }
#endif
        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="connection">数据库链接。</param>
        /// <param name="commandSql">命令。</param>
        /// <returns></returns>
        public IEnumerable<T> Query<T>(IDbConnection connection, CommandSql commandSql)
        {
            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                connection.Open();
            }

            List<T> results = new List<T>();

            CommandBehavior behavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

            if (isClosedConnection)
            {
                behavior |= CommandBehavior.CloseConnection;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandSql.Sql;
                command.CommandType = CommandType.Text;

                if (commandSql.CommandTimeout.HasValue)
                {
                    command.CommandTimeout = commandSql.CommandTimeout.Value;
                }

                foreach (var item in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, item.Key, item.Value);
                }

                using (var reader = command.ExecuteReader(behavior))
                {
                    var adaper = Adapters.GetOrAdd(reader.GetType(), type => new MapAdaper(type));

                    var map = adaper.CreateMap<T>();

                    while (reader.Read())
                    {
                        results.Add(map.Map(reader));
                    }
                }
            }

            return results;
        }


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


#if NETSTANDARD2_1_OR_GREATER
        private sealed class AsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly string connectionString;
            private readonly IDbConnectionLtsAdapter adapter;
            private readonly CommandSql commandSql;

            public AsyncEnumerable(string connectionString, IDbConnectionLtsAdapter adapter, CommandSql commandSql)
            {
                this.connectionString = connectionString;
                this.adapter = adapter;
                this.commandSql = commandSql;
            }

            public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                using (var connection = TransactionConnections.GetConnection(connectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionString, adapter, true))
                {
                    if (!(connection is DbConnection dbConnection))
                    {
                        throw new InvalidOperationException("Async operations require use of a DbConnection or an already-open IDbConnection");
                    }

                    await dbConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    CommandBehavior behavior = CommandBehavior.SequentialAccess
                        | CommandBehavior.SingleResult
                        | CommandBehavior.CloseConnection;

                    using (var command = dbConnection.CreateCommand())
                    {
                        command.CommandText = commandSql.Sql;
                        command.CommandType = CommandType.Text;

                        if (commandSql.CommandTimeout.HasValue)
                        {
                            command.CommandTimeout = commandSql.CommandTimeout.Value;
                        }

                        foreach (var item in commandSql.Parameters)
                        {
                            LookupDb.AddParameterAuto(command, item.Key, item.Value);
                        }

                        using (var reader = await command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false))
                        {
                            var adaper = Adapters.GetOrAdd(reader.GetType(), type => new MapAdaper(type));

                            var map = adaper.CreateMap<T>();

                            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            {
                                yield return map.Map(reader);
                            }
                        }
                    }
                }

                yield break;
            }
        }
#else
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
            private DbConnection dbConnection;
            private DbDataReader dbReader;
            private DbCommand dbCommand;
            private DbMapper<T> dbMapper;
            private bool isClosed = false;

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

            public T Current => dbReader.IsClosed ? default : dbMapper.Map(dbReader);

#if NETSTANDARD2_1_OR_GREATER
            public async ValueTask<bool> MoveNextAsync()
#else
            public async Task<bool> MoveNextAsync()
#endif
            {
                if (dbReader is null)
                {
                    var connection = TransactionConnections.GetConnection(connectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionString, adapter, true);

                    if (!(connection is DbConnection dbConnection))
                    {
                        throw new InvalidOperationException("Async operations require use of a DbConnection or an already-open IDbConnection");
                    }

                    this.dbConnection = dbConnection;

                    await dbConnection.OpenAsync(cancellationToken)
                        .ConfigureAwait(false);

                    CommandBehavior behavior = CommandBehavior.SequentialAccess
                        | CommandBehavior.SingleResult
                        | CommandBehavior.CloseConnection;

                    dbCommand = dbConnection.CreateCommand();

                    dbCommand.CommandText = commandSql.Sql;
                    dbCommand.CommandType = CommandType.Text;

                    if (commandSql.CommandTimeout.HasValue)
                    {
                        dbCommand.CommandTimeout = commandSql.CommandTimeout.Value;
                    }

                    foreach (var item in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(dbCommand, item.Key, item.Value);
                    }

                    dbReader = await dbCommand.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);

                    var adaper = Adapters.GetOrAdd(dbReader.GetType(), type => new MapAdaper(type));

                    dbMapper = adaper.CreateMap<T>();
                }

                if (await dbReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }

                if (!isClosed)
                {
                    dbReader.Close();
                    dbCommand.Dispose();
                    dbConnection.Close();
                    dbConnection.Dispose();

                    isClosed = true;
                }

                return false;
            }

#if NETSTANDARD2_1_OR_GREATER
            public ValueTask DisposeAsync() => new ValueTask(Task.Run(enumerator.Dispose));
#endif

            public void Dispose()
            {
                if (!isClosed)
                {
                    dbReader.Close();
                    dbCommand.Dispose();
                    dbConnection.Close();
                    dbConnection.Dispose();

                    isClosed = true;
                }
            }
        }
#endif
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
            bool isClosedConnection = connection.State == ConnectionState.Closed;

            CommandBehavior behavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow;

            if (isClosedConnection)
            {
                behavior |= CommandBehavior.CloseConnection;
            }

            if (isClosedConnection)
            {
                connection.Open();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandSql.Sql;
                command.CommandType = CommandType.Text;

                if (commandSql.CommandTimeout.HasValue)
                {
                    command.CommandTimeout = commandSql.CommandTimeout.Value;
                }

                foreach (var item in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, item.Key, item.Value);
                }

                using (var reader = command.ExecuteReader(behavior))
                {
                    if (reader.Read())
                    {
                        var adaper = Adapters.GetOrAdd(reader.GetType(), type => new MapAdaper(type));

                        var map = adaper.CreateMap<T>();

                        return map.Map(reader);
                    }
                }
            }

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
            if (!(connection is DbConnection dbConnection))
            {
                throw new InvalidOperationException("Async operations require use of a DbConnection or an already-open IDbConnection");
            }

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            CommandBehavior behavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow;

            if (isClosedConnection)
            {
                behavior |= CommandBehavior.CloseConnection;
            }

            if (isClosedConnection)
            {
                await dbConnection.OpenAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            using (var command = dbConnection.CreateCommand())
            {
                command.CommandText = commandSql.Sql;
                command.CommandType = CommandType.Text;

                if (commandSql.CommandTimeout.HasValue)
                {
                    command.CommandTimeout = commandSql.CommandTimeout.Value;
                }

                foreach (var item in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, item.Key, item.Value);
                }

                using (var reader = await command.ExecuteReaderAsync(behavior).ConfigureAwait(false))
                {
                    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var adaper = Adapters.GetOrAdd(reader.GetType(), type => new MapAdaper(type));

                        var map = adaper.CreateMap<T>();

                        return map.Map(reader);
                    }
                }
            }

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

        private static readonly MethodInfo Map;
        private static readonly MethodInfo MapGeneric;
        private static readonly ConcurrentDictionary<Type, MapAdaper> Adapters = new ConcurrentDictionary<Type, MapAdaper>();

        static DapperFor()
        {
            var methodInfos = typeof(Mapper).GetMethods();

            Map = methodInfos.Single(x => x.Name == nameof(Mapper.Map) && !x.IsGenericMethod);
            MapGeneric = methodInfos.Single(x => x.Name == nameof(Mapper.Map) && x.IsGenericMethod);
        }

        private class MapAdaper
        {
            private readonly Type type;
            private readonly MethodInfo isDbNull;
            private readonly MethodInfo getName;
            private readonly MethodInfo getValue;
            private readonly MethodInfo getFieldType;
            private readonly Dictionary<Type, MethodInfo> typeMap;

            public MethodInfo EqualMethod { get; }

            private static bool Equals(string a, string b)
            {
                return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
            }

            public MapAdaper(Type type)
            {
                var types = new Type[] { typeof(int) };

                this.type = type;

                getName = type.GetMethod("GetName", types);

                getValue = type.GetMethod("GetValue", types);

                isDbNull = type.GetMethod("IsDBNull", types);

                getFieldType = type.GetMethod("GetFieldType", types);

                typeMap = new Dictionary<Type, MethodInfo>
                {
                    [typeof(bool)] = type.GetMethod("GetBoolean", types),
                    [typeof(byte)] = type.GetMethod("GetByte", types),
                    [typeof(char)] = type.GetMethod("GetChar", types),
                    [typeof(short)] = type.GetMethod("GetInt16", types),
                    [typeof(int)] = type.GetMethod("GetInt32", types),
                    [typeof(long)] = type.GetMethod("GetInt64", types),
                    [typeof(float)] = type.GetMethod("GetFloat", types),
                    [typeof(double)] = type.GetMethod("GetDouble", types),
                    [typeof(decimal)] = type.GetMethod("GetDecimal", types),
                    [typeof(Guid)] = type.GetMethod("GetGuid", types),
                    [typeof(DateTime)] = type.GetMethod("GetDateTime", types),
                    [typeof(string)] = type.GetMethod("GetString", types),
                    [typeof(object)] = type.GetMethod("GetValue", types)
                };

                EqualMethod = typeof(MapAdaper).GetMethod(nameof(Equals), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            }

            public ParameterExpression DbVariable() => Parameter(type);

            public UnaryExpression Convert(ParameterExpression parameterExp) => Expression.Convert(parameterExp, type);

            public Expression ToSolve(Type propertyType, ParameterExpression dbVar, Expression iVar)
            {
                if (typeMap.TryGetValue(propertyType, out MethodInfo methodInfo))
                {
                    return Condition(Equal(Call(dbVar, getFieldType, iVar), Constant(propertyType))
                            , Call(dbVar, methodInfo, iVar)
                            , Call(MapGeneric.MakeGenericMethod(propertyType), Call(dbVar, getValue, iVar), Default(propertyType)));
                }

                return Call(MapGeneric.MakeGenericMethod(propertyType), Call(dbVar, getValue, iVar), Default(propertyType));
            }

            public Expression IsDbNull(ParameterExpression dbVar, Expression iVar) => Call(dbVar, isDbNull, iVar);

            public Expression GetName(ParameterExpression dbVar, Expression iVar) => Call(dbVar, getName, iVar);

            public DbMapper<T> CreateMap<T>() => DbEngine<T>.CreateMap(this);
        }

        private class DbEngine<T>
        {
            private static readonly ConcurrentDictionary<MapAdaper, DbMapper<T>> Mappers = new ConcurrentDictionary<MapAdaper, DbMapper<T>>();

            public static DbMapper<T> CreateMap(MapAdaper adaper)
                => Mappers.GetOrAdd(adaper, db => new DbMapperGen<T>(db).CreateMap());
        }

        private class DbMapperGen<T>
        {
            private readonly MapAdaper adaper;

            public DbMapperGen(MapAdaper adaper)
            {
                this.adaper = adaper;
            }

            private Func<IDataReader, T> MakeSimple(Type type)
            {
                var paramterExp = Parameter(typeof(IDataReader));

                var iVar = Constant(0);

                var dbVar = adaper.DbVariable();

                var bodyExp = UncheckedValue(type, dbVar, iVar);

                var lambdaExp = Lambda<Func<IDataReader, T>>(Block(new ParameterExpression[] { dbVar }, Assign(dbVar, adaper.Convert(paramterExp)), Condition(adaper.IsDbNull(dbVar, iVar), Default(type), bodyExp)), paramterExp);

                return lambdaExp.Compile();
            }

            private Func<IDataReader, T> MakeSimpleNull(Type type, Type nullableType)
            {
                var paramterExp = Parameter(typeof(IDataReader));

                var iVar = Constant(0);

                var dbVar = adaper.DbVariable();

                var bodyExp = UncheckedValue(type, dbVar, iVar);

                var lambdaExp = Lambda<Func<IDataReader, T>>(Block(new ParameterExpression[] { dbVar }, Assign(dbVar, adaper.Convert(paramterExp)), Condition(adaper.IsDbNull(dbVar, iVar), New(nullableType.GetConstructor(Type.EmptyTypes)), bodyExp)), paramterExp);

                return lambdaExp.Compile();
            }

            private Func<IDataReader, T> MakeNull(Type type, Type nullableType)
            {
                var nullCtor = nullableType.GetConstructor(new Type[] { type });

                //? 无参构造函数。
                var nonCtor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);

                if (nonCtor is null)
                {
                    var constructorInfos = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                    foreach (var constructorInfo in constructorInfos)
                    {
                        return MakeFor(constructorInfo, newExp => New(nullCtor, newExp));
                    }
                }

                return MakeFor(type, nonCtor, instanceExp => New(nullCtor, instanceExp));
            }

            private Func<IDataReader, T> MakeFor(Type type, ConstructorInfo constructorInfo, Func<ParameterExpression, Expression> convert)
            {
                var instanceExp = Variable(type);

                var paramterExp = Parameter(typeof(IDataReader));

                var dbVar = adaper.DbVariable();

                var iVar = Parameter(typeof(int));

                var lenVar = Property(dbVar, "FieldCount");

                var list = new List<Expression>
                {
                    Assign(iVar, Constant(0)),
                    Assign(dbVar, adaper.Convert(paramterExp)),
                    Assign(instanceExp, New(constructorInfo))
                };

                var listCases = new List<SwitchCase>();

                foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!propertyInfo.CanWrite)
                    {
                        continue;
                    }

                    var propertyItem = PropertyItem.Get(propertyInfo);

                    if (propertyItem.Ignore)
                    {
                        continue;
                    }

                    listCases.Add(SwitchCaseAssign(instanceExp, propertyItem, dbVar, iVar));
                }

                LabelTarget break_label = Label(typeof(void));
                LabelTarget continue_label = Label(typeof(void));

                var body = Switch(adaper.GetName(dbVar, iVar), null, adaper.EqualMethod, listCases);

                list.Add(Loop(IfThenElse(
                     LessThan(iVar, lenVar),
                     Block(
                         body,
                         AddAssign(iVar, Constant(1)),
                         Continue(continue_label, typeof(void))
                     ),
                     Break(break_label, typeof(void)))
                    , break_label, continue_label));

                list.Add(convert.Invoke(instanceExp));

                var lambdaExp = Lambda<Func<IDataReader, T>>(Block(new ParameterExpression[] { iVar, dbVar, instanceExp }, list), paramterExp);

                return lambdaExp.Compile();
            }

            private Func<IDataReader, T> MakeNoArgumentsCtor(Type type, ConstructorInfo constructorInfo)
                => MakeFor(type, constructorInfo, instanceExp => instanceExp);

            private Func<IDataReader, T> MakeFor(ConstructorInfo constructorInfo, Func<NewExpression, Expression> convert)
            {
                var paramterExp = Parameter(typeof(IDataReader));

                var dbVar = adaper.DbVariable();

                var parameterInfos = constructorInfo.GetParameters();

                var arguments = new List<Expression>(parameterInfos.Length);

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    var parameterInfo = parameterInfos[i];

                    var iVar = Constant(i);

                    var uncheckedValue = UncheckedValue(parameterInfo.ParameterType, dbVar, iVar);

                    arguments.Add(Condition(adaper.IsDbNull(dbVar, iVar), Default(parameterInfo.ParameterType), uncheckedValue));
                }

                var lambdaExp = Lambda<Func<IDataReader, T>>(Block(new ParameterExpression[] { dbVar }, Assign(dbVar, adaper.Convert(paramterExp)), convert.Invoke(New(constructorInfo, arguments))), paramterExp);

                return lambdaExp.Compile();
            }

            private Func<IDataReader, T> MakeCtor(ConstructorInfo constructorInfo)
                => MakeFor(constructorInfo, newExp => newExp);

            private Expression UncheckedValue(Type type, ParameterExpression dbVar, Expression iVar)
            {
                bool isEnum = false;
                bool isNullable = false;

                Type propertyType = type;
                Type nonullableType = type;

                if (propertyType.IsValueType)
                {
                    if (propertyType.IsNullable())
                    {
                        isNullable = true;

                        propertyType = nonullableType = Nullable.GetUnderlyingType(propertyType);
                    }

                    if (propertyType.IsEnum)
                    {
                        isEnum = true;

                        propertyType = Enum.GetUnderlyingType(propertyType);
                    }
                }

                Expression body = adaper.ToSolve(propertyType, dbVar, iVar);

                if (isEnum)
                {
                    body = Convert(body, nonullableType);
                }

                if (isNullable)
                {
                    body = New(type.GetConstructor(new Type[] { nonullableType }), body);
                }

                return body;
            }

            private SwitchCase SwitchCaseAssign(Expression instanceExp, PropertyItem propertyItem, ParameterExpression dbVar, ParameterExpression iVar)
            {
                Expression body = UncheckedValue(propertyItem.MemberType, dbVar, iVar);

                return SwitchCase(IfThen(Not(adaper.IsDbNull(dbVar, iVar)), Assign(Property(instanceExp, propertyItem.Member), body)), Constant(propertyItem.Naming), Constant(propertyItem.Name));
            }

            public DbMapper<T> CreateMap()
            {
                var type = typeof(T);

                if (type.IsSimpleType())
                {
                    return new DbMapper<T>(MakeSimple(type));
                }

                if (type.IsNullable())
                {
                    var conversionType = Enum.GetUnderlyingType(type);

                    if (conversionType.IsSimpleType())
                    {
                        return new DbMapper<T>(MakeSimpleNull(conversionType, type));
                    }

                    return new DbMapper<T>(MakeNull(conversionType, type));
                }

                //? 无参构造函数。
                var nonCtor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);

                if (nonCtor is null)
                {
                    var constructorInfos = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                    foreach (var constructorInfo in constructorInfos)
                    {
                        return new DbMapper<T>(MakeCtor(constructorInfo));
                    }
                }

                return new DbMapper<T>(MakeNoArgumentsCtor(type, nonCtor));
            }
        }
        private class DbMapper<T>
        {
            private readonly Func<IDataReader, T> read;

            public DbMapper(Func<IDataReader, T> read)
            {
                this.read = read;
            }

            public T Map(IDataReader reader) => read.Invoke(reader);
        }
    }
}

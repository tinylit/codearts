using CodeArts.Db.Exceptions;
using CodeArts.Db.Routes;
using CodeArts.Runtime;
using Dapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Dapper
{
    /// <summary>
    /// 数据操作。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public class DbSet<TEntity> where TEntity : class, IEntiy
    {
        private static readonly ITableInfo tableInfo;

        private readonly string connectionString;
        private readonly IDbConnectionAdapter connectionAdapter;

        static DbSet() => tableInfo = TableRegions.Resolve<TEntity>();

        /// <summary>
        /// inheritdoc
        /// </summary>
        public DbSet(IDbConnectionAdapter connectionAdapter, string connectionString)
        {
            this.connectionAdapter = connectionAdapter ?? throw new ArgumentNullException(nameof(connectionAdapter));
            this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private static object FixParameters(Dictionary<string, ParameterValue> parameters)
        {
            var results = new DynamicParameters();

            foreach (var kv in parameters)
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

        /// <summary>
        /// 最大参数长度。
        /// </summary>
        public const int MAX_PARAMETERS_COUNT = 1000;

        /// <summary>
        /// 最大IN SQL 参数长度（取自 Oracle 9I）。
        /// </summary>
        public const int MAX_IN_SQL_PARAMETERS_COUNT = 256;

        /// <summary>
        /// 数据验证。
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="validationContext">验证上下文。</param>
        /// <param name="validationAttributes">验证属性集合。</param>
        private static void ValidateValue(object value, ValidationContext validationContext, IEnumerable<ValidationAttribute> validationAttributes)
        {
            //? 内容为空的时候不进行【数据类型和正则表达式验证】，内容空验证请使用【RequiredAttribute】。
            if (value is string text && text.IsEmpty())
            {
                validationAttributes = validationAttributes
                    .Except(validationAttributes.OfType<DataTypeAttribute>())
                    .Except(validationAttributes.OfType<RegularExpressionAttribute>());
            }

            DbValidator.ValidateValue(value, validationContext, validationAttributes);
        }

        /// <summary>
        /// 路由执行力。
        /// </summary>
        private abstract class DbRouteExecuter
        {
            protected static readonly TypeItem typeItem;
            static DbRouteExecuter()
            {
                typeItem = TypeItem.Get(typeof(TEntity));
            }

            private Action<CommandSql> watchSql;
            private IsolationLevel? isolationLevel;
            private readonly IDbConnectionAdapter connectionAdapter;
            private readonly string connectionString;
            private readonly ICollection<TEntity> entries;

            protected DbRouteExecuter(DbRouteExecuter executer)
            {
                watchSql = executer.watchSql;
                isolationLevel = executer.isolationLevel;

                entries = executer.entries;
                connectionString = executer.connectionString;
                connectionAdapter = executer.connectionAdapter;
            }

            protected DbRouteExecuter(IDbConnectionAdapter connectionAdapter, string connectionString, ICollection<TEntity> entries)
            {
                this.entries = entries ?? throw new ArgumentNullException(nameof(entries));
                this.connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
                this.connectionAdapter = connectionAdapter ?? throw new ArgumentNullException(nameof(connectionAdapter));
            }

            public IDbRouter<TEntity> DbRouter => DbRouter<TEntity>.Instance;

            public ISQLCorrectSettings Settings => connectionAdapter.Settings;
            private IDbConnection CreateDb(bool useCache = true) => TransactionConnections.GetConnection(connectionString, connectionAdapter) ?? DispatchConnections.Instance.GetConnection(connectionString, connectionAdapter, useCache);

            public int ExecuteCommand(int? commandTimeout = null)
            {
                if (entries.Count == 0)
                {
                    return 0;
                }

                if (!IsValid())
                {
                    throw new DException("信息校验失败!");
                }

                var results = PrepareCommand(entries);

                if (results.Count == 0)
                {
                    throw new NotSupportedException();
                }

                return Execute(results, commandTimeout);
            }

            private int Execute(List<Tuple<string, Dictionary<string, ParameterValue>>> results, int? commandTimeout = null)
            {
                int influenceLine = 0;

                if (commandTimeout.HasValue)
                {
                    Stopwatch stopwatch = new Stopwatch();

                    if (isolationLevel.HasValue)
                    {
                        using (var connection = CreateDb())
                        {
                            connection.Open();

                            using (var transaction = connection.BeginTransaction(isolationLevel.Value))
                            {
                                results.ForEach(x =>
                                {
                                    int? timeOut = commandTimeout - (int)(stopwatch.ElapsedMilliseconds / 1000L);

                                    watchSql?.Invoke(new CommandSql(x.Item1, x.Item2, timeOut));

                                    stopwatch.Start();

                                    influenceLine += connection.Execute(x.Item1, FixParameters(x.Item2), transaction, timeOut);

                                    stopwatch.Stop();
                                });
                            }

                            connection.Close();
                        }

                        return influenceLine;
                    }

                    using (var connection = CreateDb())
                    {
                        connection.Open();

                        results.ForEach(x =>
                        {
                            int? timeOut = commandTimeout - (int)(stopwatch.ElapsedMilliseconds / 1000L);

                            watchSql?.Invoke(new CommandSql(x.Item1, x.Item2, timeOut));

                            stopwatch.Start();

                            influenceLine += connection.Execute(x.Item1, FixParameters(x.Item2), null, timeOut);

                            stopwatch.Stop();
                        });

                        connection.Close();
                    }

                    return influenceLine;
                }

                if (isolationLevel.HasValue)
                {
                    using (var connection = CreateDb())
                    {
                        connection.Open();

                        using (var transaction = connection.BeginTransaction(isolationLevel.Value))
                        {
                            results.ForEach(x =>
                            {
                                watchSql?.Invoke(new CommandSql(x.Item1, x.Item2, commandTimeout));

                                influenceLine += connection.Execute(x.Item1, FixParameters(x.Item2), transaction);
                            });
                        }

                        connection.Close();
                    }

                    return influenceLine;
                }

                using (var connection = CreateDb())
                {
                    connection.Open();

                    results.ForEach(x =>
                    {
                        watchSql?.Invoke(new CommandSql(x.Item1, x.Item2, commandTimeout));

                        influenceLine += connection.Execute(x.Item1, FixParameters(x.Item2));
                    });

                    connection.Close();
                }

                return influenceLine;
            }

            protected void SetWatchSql(Action<CommandSql> watchSql)
            {
                this.watchSql = watchSql;
            }

            protected void SetTransaction(IsolationLevel isolationLevel)
            {
                this.isolationLevel = isolationLevel;
            }

            public abstract bool IsValid();

            public abstract List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand(ICollection<TEntity> entries);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            public Task<int> ExecuteCommandAsync(CancellationToken cancellationToken = default) => ExecuteCommandAsync(null, cancellationToken);

            public Task<int> ExecuteCommandAsync(int? commandTimeout, CancellationToken cancellationToken = default)
            {
                if (entries.Count == 0)
                {
                    return Task.FromResult(0);
                }

                var results = PrepareCommand(entries);

                if (results.Count == 0)
                {
                    return Task.FromResult(0);
                }

                return ExecutedAsync(results, commandTimeout, cancellationToken);
            }

            private async Task<int> ExecutedAsync(List<Tuple<string, Dictionary<string, ParameterValue>>> results, int? commandTimeout, CancellationToken cancellationToken)
            {
                int influenceLine = 0;

                if (commandTimeout.HasValue)
                {
                    Stopwatch stopwatch = new Stopwatch();

                    if (isolationLevel.HasValue)
                    {
                        using (var connection = CreateDb())
                        {
                            if (connection is DbConnection dbConnection)
                            {
                                await dbConnection.OpenAsync(cancellationToken)
                                    .ConfigureAwait(false);

#if NETSTANDARD2_1_OR_GREATER
                                using (var transaction = await dbConnection.BeginTransactionAsync(isolationLevel.Value)
                                    .ConfigureAwait(false))
                                {
                                    foreach (var x in results)
                                    {
                                        int? timeOut = commandTimeout - (int)(stopwatch.ElapsedMilliseconds / 1000L);

                                        watchSql?.Invoke(new CommandSql(x.Item1, x.Item2, timeOut));

                                        stopwatch.Start();

                                        influenceLine += await connection.ExecuteAsync(new CommandDefinition(x.Item1, FixParameters(x.Item2), transaction, timeOut, CommandType.Text, CommandFlags.Buffered, cancellationToken));

                                        stopwatch.Stop();
                                    }
                                }

                                await dbConnection.CloseAsync()
                                    .ConfigureAwait(false);
#else
                                using (var transaction = dbConnection.BeginTransaction(isolationLevel.Value))
                                {
                                    foreach (var x in results)
                                    {
                                        int? timeOut = commandTimeout - (int)(stopwatch.ElapsedMilliseconds / 1000L);

                                        watchSql?.Invoke(new CommandSql(x.Item1, x.Item2, timeOut));

                                        stopwatch.Start();

                                        influenceLine += await connection.ExecuteAsync(new CommandDefinition(x.Item1, FixParameters(x.Item2), transaction, timeOut, CommandType.Text, CommandFlags.Buffered, cancellationToken));

                                        stopwatch.Stop();
                                    }
                                }

                                dbConnection.Close();
#endif

                                return influenceLine;
                            }

                            connection.Open();

                            using (var transaction = connection.BeginTransaction(isolationLevel.Value))
                            {
                                results.ForEach(x =>
                               {
                                   int? timeOut = commandTimeout - (int)(stopwatch.ElapsedMilliseconds / 1000L);

                                   watchSql?.Invoke(new CommandSql(x.Item1, x.Item2, timeOut));

                                   stopwatch.Start();

                                   influenceLine += connection.Execute(x.Item1, FixParameters(x.Item2), transaction, timeOut);

                                   stopwatch.Stop();
                               });
                            }

                            connection.Close();

                            return influenceLine;
                        }
                    }

                    using (var connection = CreateDb())
                    {
                        connection.Open();

                        results.ForEach(x =>
                        {
                            int? timeOut = commandTimeout - (int)(stopwatch.ElapsedMilliseconds / 1000L);

                            watchSql?.Invoke(new CommandSql(x.Item1, x.Item2, timeOut));

                            stopwatch.Start();

                            influenceLine += connection.Execute(x.Item1, FixParameters(x.Item2), null, timeOut);

                            stopwatch.Stop();
                        });

                        connection.Close();
                    }

                    return influenceLine;
                }

                using (var connection = CreateDb())
                {
                    if (connection is DbConnection dbConnection)
                    {
                        await dbConnection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

#if NETSTANDARD2_1_OR_GREATER
                        foreach (var x in results)
                        {
                            watchSql?.Invoke(new CommandSql(x.Item1, x.Item2));

                            influenceLine += await connection.ExecuteAsync(new CommandDefinition(x.Item1, FixParameters(x.Item2), null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
                        }

                        await dbConnection.CloseAsync()
                            .ConfigureAwait(false);
#else
                        foreach (var x in results)
                        {
                            watchSql?.Invoke(new CommandSql(x.Item1, x.Item2, commandTimeout));

                            influenceLine += await connection.ExecuteAsync(new CommandDefinition(x.Item1, FixParameters(x.Item2), null, commandTimeout, CommandType.Text, CommandFlags.Buffered, cancellationToken));
                        }

                        dbConnection.Close();
#endif

                        return influenceLine;
                    }

                    connection.Open();

                    results.ForEach(x =>
                    {
                        watchSql?.Invoke(new CommandSql(x.Item1, x.Item2));

                        influenceLine += connection.Execute(x.Item1, FixParameters(x.Item2));
                    });

                    connection.Close();

                    return influenceLine;
                }
            }
#endif
        }

        private class MyStringComparer : Singleton<MyStringComparer>, IComparer<string>
        {
            private MyStringComparer() { }

            public int Compare(string x, string y)
            {
                if (x == "Id")
                {
                    return -1;
                }

                if (y == "Id")
                {
                    return 1;
                }

                return string.Compare(x, y);
            }
        }

        #region Insert
        private class Insertable : DbRouteExecuter, IInsertable<TEntity>
        {
            private static readonly SortedDictionary<string, string> inserts;
            static Insertable()
            {
                inserts = new SortedDictionary<string, string>(MyStringComparer.Instance);

                foreach (var kv in tableInfo.ReadWrites)
                {
                    inserts[kv.Key] = kv.Value;
                }
            }

            protected readonly Dictionary<string, string> insertColumns;

            protected Insertable(Insertable insertable) : base(insertable)
            {
                insertColumns = insertable.insertColumns;
            }

            public Insertable(IDbConnectionAdapter connectionAdapter, string connectionString, ICollection<TEntity> entries) : base(connectionAdapter, connectionString, entries)
            {
                insertColumns = new Dictionary<string, string>(inserts, StringComparer.OrdinalIgnoreCase);
            }

            protected Tuple<string, Dictionary<string, ParameterValue>> SqlGenerator(string tableName, IEnumerable<TEntity> list, Dictionary<string, string> cols)
            {
                var sb = new StringBuilder();
                var parameters = new Dictionary<string, ParameterValue>();

                sb.Append("INSERT INTO ")
                    .Append(Settings.Name(tableName))
                    .Append("(")
                    .Append(string.Join(",", cols.Select(x => Settings.Name(x.Value))))
                    .Append(")")
                    .Append(" VALUES ")
                    .Append(string.Join(",", list.Select((item, index) =>
                    {
                        var context = new ValidationContext(item, null, null);

                        return string.Concat("(", string.Join(",", cols.Select(kv =>
                        {
                            var storeItem = typeItem.PropertyStores.First(x => x.Name == kv.Key);

                            var parameterKey = index == 0 ?
                                kv.Key.ToUrlCase()
                                :
                                $"{kv.Key.ToUrlCase()}_{index}";

                            var value = storeItem.Member.GetValue(item, null);

                            if (value is null || storeItem.MemberType.IsValueType && Equals(value, Emptyable.Empty(storeItem.MemberType)))
                            {
                                if (tableInfo.Tokens.TryGetValue(kv.Key, out TokenAttribute token))
                                {
                                    value = token.Create();

                                    if (value is null)
                                    {
                                        throw new NoNullAllowedException("令牌不允许为空!");
                                    }
                                }
                                else if (storeItem.MemberType == Types.DateTime)
                                {
                                    value = DateTime.Now;
                                }
                                else if (storeItem.MemberType == Types.Guid)
                                {
                                    value = Guid.NewGuid();
                                }
                                else if (storeItem.MemberType == Types.DateTimeOffset)
                                {
                                    value = DateTimeOffset.Now;
                                }
                                else if (storeItem.MemberType == Types.Version)
                                {
                                    value = new Version(1, 0, 0, 0);
                                }
                                else
                                {
                                    goto label_valid;
                                }

                                storeItem.Member.SetValue(item, value, null); //? 刷新数据。
                            }

label_valid:

                            context.MemberName = storeItem.Member.Name;

                            var attrs = storeItem.Attributes.OfType<ValidationAttribute>();

                            if (attrs.Any())
                            {
                                ValidateValue(value, context, attrs);
                            }

                            parameters.Add(parameterKey, ParameterValue.Create(value, storeItem.MemberType));

                            return Settings.ParamterName(parameterKey);

                        })), ")");
                    })));

                return new Tuple<string, Dictionary<string, ParameterValue>>(sb.ToString(), parameters);
            }

            public override bool IsValid() => insertColumns.Count > 0;

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand(ICollection<TEntity> entries)
            {
                string tableName = tableInfo.TableName;

                if (Settings.Engine == DatabaseEngine.Oracle)
                {
                    var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(entries.Count);

                    foreach (var entity in entries)
                    {
                        results.Add(SqlGenerator(tableName, new TEntity[] { entity }, insertColumns));
                    }

                    return results;
                }
                else
                {
                    var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(1);

                    int parameterCount = entries.Count * insertColumns.Count;

                    if (parameterCount <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                    {
                        results.Add(SqlGenerator(tableName, entries, insertColumns));
                    }
                    else
                    {
                        int offset = MAX_PARAMETERS_COUNT / insertColumns.Count;

                        for (int i = 0; i < entries.Count; i += offset)
                        {
                            results.Add(SqlGenerator(tableName, entries.Skip(i).Take(offset), insertColumns));
                        }
                    }

                    return results;
                }
            }

            private void Aw_Limit(string[] columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                var keys = new List<string>(columns.Length);

                foreach (var kv in insertColumns.Where(x => !columns.Contains(x.Key) && !columns.Contains(x.Value)))
                {
                    keys.Add(kv.Key);
                }

                if (insertColumns.Count == keys.Count)
                {
                    throw new DException("未指定插入字段!");
                }

                for (int i = 0, length = keys.Count; i < length; i++)
                {
                    if (tableInfo.Tokens.ContainsKey(keys[i]))
                    {
                        continue;
                    }

                    insertColumns.Remove(keys[i]);
                }
            }
            private void Aw_Except(string[] columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                var keys = new List<string>(columns.Length);

                foreach (var kv in insertColumns.Where(x => columns.Contains(x.Key) || columns.Contains(x.Value)))
                {
                    keys.Add(kv.Key);
                }

                if (insertColumns.Count == keys.Count)
                {
                    throw new DException("未指定插入字段!");
                }

                for (int i = 0, length = keys.Count; i < length; i++)
                {
                    if (tableInfo.Tokens.ContainsKey(keys[i]))
                    {
                        continue;
                    }

                    insertColumns.Remove(keys[i]);
                }
            }

            public IInsertableByCommit<TEntity> Except(string[] columns)
            {
                Aw_Except(columns);

                return this;
            }

            public IInsertableByCommit<TEntity> Except<TColumn>(Expression<Func<TEntity, TColumn>> columns)
                => Except(DbRouter<TEntity>.Instance.Except(columns));

            public IInsertableByFrom<TEntity> Into(Func<ITableInfo, string> tableGetter)
                => new InsertableByFrom(this, tableGetter);

            public IInsertableByFrom<TEntity> Into(Func<ITableInfo, TEntity, string> tableGetter)
                => new InsertableByAnalyseFrom(this, tableGetter);

            public IInsertableByCommit<TEntity> Limit(string[] columns)
            {
                Aw_Limit(columns);

                return this;
            }

            public IInsertableByCommit<TEntity> Limit<TColumn>(Expression<Func<TEntity, TColumn>> columns)
                => Limit(DbRouter.Limit(columns));

            public IInsertableByCommit<TEntity> WatchSql(Action<CommandSql> watchSql)
            {
                SetWatchSql(watchSql);

                return this;
            }

            public IInsertableByCommit<TEntity> Transaction()
            {
                SetTransaction(IsolationLevel.ReadCommitted);

                return this;
            }

            public IInsertableByCommit<TEntity> Transaction(IsolationLevel isolationLevel)
            {
                SetTransaction(isolationLevel);

                return this;
            }
        }

        private class InsertableByFrom : Insertable
        {
            private readonly Func<ITableInfo, string> tableGetter;

            public InsertableByFrom(Insertable insertable, Func<ITableInfo, string> tableGetter) : base(insertable)
            {
                this.tableGetter = tableGetter;
            }

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand(ICollection<TEntity> entries)
            {
                string tableName = tableGetter.Invoke(tableInfo) ?? throw new DSyntaxErrorException("请指定表名称!");

                if (Settings.Engine == DatabaseEngine.Oracle)
                {
                    var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(entries.Count);

                    foreach (var entity in entries)
                    {
                        results.Add(SqlGenerator(tableName, new TEntity[] { entity }, insertColumns));
                    }

                    return results;
                }
                else
                {
                    int parameterCount = entries.Count * insertColumns.Count;

                    var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(parameterCount / MAX_PARAMETERS_COUNT + 1);

                    if (parameterCount <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                    {
                        results.Add(SqlGenerator(tableName, entries, insertColumns));
                    }
                    else
                    {
                        int offset = MAX_PARAMETERS_COUNT / insertColumns.Count;

                        for (int i = 0; i < entries.Count; i += offset)
                        {
                            results.Add(SqlGenerator(tableName, entries.Skip(i).Take(offset), insertColumns));
                        }
                    }

                    return results;
                }
            }
        }

        private class InsertableByAnalyseFrom : Insertable
        {
            private readonly Func<ITableInfo, TEntity, string> tableGetter;

            public InsertableByAnalyseFrom(Insertable insertable, Func<ITableInfo, TEntity, string> tableGetter) : base(insertable)
            {
                this.tableGetter = tableGetter;
            }

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand(ICollection<TEntity> entries)
            {
                if (Settings.Engine == DatabaseEngine.Oracle)
                {
                    var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(entries.Count);

                    entries.GroupBy(x => tableGetter.Invoke(tableInfo, x) ?? throw new DSyntaxErrorException("请指定表名称!"))
                        .ForEach(x =>
                        {
                            foreach (var entity in x)
                            {
                                results.Add(SqlGenerator(x.Key, new TEntity[] { entity }, insertColumns));
                            }
                        });

                    return results;
                }
                else
                {
                    var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(insertColumns.Count * entries.Count / MAX_PARAMETERS_COUNT + 1);

                    entries.GroupBy(x => tableGetter.Invoke(tableInfo, x) ?? throw new DSyntaxErrorException("请指定表名称!"))
                        .ForEach(x =>
                        {
                            int parameterCount = x.Count() * insertColumns.Count;

                            if (parameterCount <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                            {
                                results.Add(SqlGenerator(x.Key, entries, insertColumns));
                            }
                            else
                            {
                                int offset = MAX_PARAMETERS_COUNT / insertColumns.Count;

                                for (int i = 0; i < entries.Count; i += offset)
                                {
                                    results.Add(SqlGenerator(x.Key, x.Skip(i).Take(offset), insertColumns));
                                }
                            }
                        });

                    return results;
                }
            }
        }
        #endregion

        #region Delete
        private class Deleteable : DbRouteExecuter, IDeleteable<TEntity>
        {
            private static readonly SortedDictionary<string, string> deletes;

            static Deleteable()
            {
                deletes = new SortedDictionary<string, string>(MyStringComparer.Instance);

                foreach (var key in tableInfo.Keys.Union(tableInfo.Tokens.Keys))
                {
                    deletes.Add(key, tableInfo.ReadOrWrites[key]);
                }
            }

            protected readonly Dictionary<string, string> whereColumns;

            protected Deleteable(Deleteable deleteable) : base(deleteable)
            {
                whereColumns = deleteable.whereColumns;
            }

            public Deleteable(IDbConnectionAdapter connectionAdapter, string connectionString, ICollection<TEntity> entries) : base(connectionAdapter, connectionString, entries)
            {
                whereColumns = new Dictionary<string, string>(deletes);
            }

            public IDeleteableByFrom<TEntity> From(Func<ITableInfo, string> tableGetter)
                => new DeleteableByFrom(this, tableGetter);

            public IDeleteableByFrom<TEntity> From(Func<ITableInfo, TEntity, string> tableGetter)
                => new DeleteableByAnalyseFrom(this, tableGetter);

            public override bool IsValid() => whereColumns.Count > 0;

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand(ICollection<TEntity> entries)
            {
                string tableName = tableInfo.TableName;

                bool singleFlag = whereColumns.Count == 1;

                int parameterCount = entries.Count * whereColumns.Count;

                List<Tuple<string, Dictionary<string, ParameterValue>>> results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(1);

                if (parameterCount <= MAX_PARAMETERS_COUNT)
                {
                    if (singleFlag)
                    {
                        results.Add(Simple(tableName, whereColumns.First(), entries));
                    }
                    else
                    {
                        results.Add(Complex(tableName, whereColumns, entries));
                    }
                }
                else
                {
                    int offset = MAX_PARAMETERS_COUNT / whereColumns.Count;

                    for (int i = 0; i < entries.Count; i += offset)
                    {
                        if (singleFlag)
                        {
                            results.Add(Simple(tableName, whereColumns.First(), entries.Skip(i).Take(offset)));
                        }
                        else
                        {
                            results.Add(Complex(tableName, whereColumns, entries.Skip(i).Take(offset)));
                        }
                    }
                }

                return results;
            }

            private void Aw_Where(string[] columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                for (int i = 0, length = columns.Length; i < length; i++)
                {
                    bool flag = true;

                    string key = columns[i];

                    foreach (var kv in tableInfo.ReadOrWrites)
                    {
                        if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase) || string.Equals(kv.Value, key, StringComparison.OrdinalIgnoreCase))
                        {
                            whereColumns[kv.Key] = kv.Value;

                            flag = false;

                            break;
                        }
                    }

                    if (flag)
                    {
                        throw new DSyntaxErrorException($"未找到字段({key})!");
                    }
                }
            }

            public IDeleteableByWhere<TEntity> Where(string[] columns)
            {
                Aw_Where(columns);

                return this;
            }

            public IDeleteableByWhere<TEntity> Where<TColumn>(Expression<Func<TEntity, TColumn>> columns)
                => Where(DbRouter.Where(columns));

            public IDeleteableByCommit<TEntity> Transaction()
            {
                SetTransaction(IsolationLevel.ReadCommitted);

                return this;
            }

            public IDeleteableByCommit<TEntity> Transaction(IsolationLevel isolationLevel)
            {
                SetTransaction(isolationLevel);

                return this;
            }

            protected Tuple<string, Dictionary<string, ParameterValue>> Simple(string tableName, KeyValuePair<string, string> col, IEnumerable<TEntity> entities)
            {
                var sb = new StringBuilder();

                string name = col.Key.ToUrlCase();

                var storeItem = typeItem.PropertyStores.First(x => x.Name == col.Key);

                sb.Append("DELETE FROM ")
                .Append(Settings.Name(tableName))
                .Append(" WHERE ")
                .Append(col.Value);

                var parameters = new Dictionary<string, ParameterValue>(entities.Count());

                var list = entities.Select((item, index) =>
                 {
                     var context = new ValidationContext(item, null, null)
                     {
                         MemberName = storeItem.Member.Name
                     };

                     var value = storeItem.Member.GetValue(item, null);

                     var attrs = storeItem.Attributes.OfType<ValidationAttribute>();

                     if (attrs.Any())
                     {
                         ValidateValue(value, context, attrs);

                         value = storeItem.Member.GetValue(item, null);
                     }

                     var parameterKey = index == 0 ?
                         name
                         :
                         $"{name}_{index}";

                     parameters.Add(parameterKey, ParameterValue.Create(value, storeItem.MemberType));

                     return Settings.ParamterName(parameterKey);

                 }).ToList();

                if (list.Count == 1)
                {
                    return new Tuple<string, Dictionary<string, ParameterValue>>(sb.Append("=")
                        .Append(list[0])
                        .ToString(), parameters);
                }

                if (list.Count <= MAX_IN_SQL_PARAMETERS_COUNT)
                {
                    return new Tuple<string, Dictionary<string, ParameterValue>>(sb.Append(" IN (")
                     .Append(string.Join(",", list))
                     .Append(")")
                     .ToString(), parameters);
                }

                for (int i = 0; i < list.Count; i += MAX_IN_SQL_PARAMETERS_COUNT)
                {
                    if (i > 0)
                    {
                        sb.Append("DELETE FROM ")
                            .Append(Settings.Name(tableName))
                            .Append(" WHERE ")
                            .Append(col.Value);
                    }

                    sb.Append(" IN (")
                     .Append(string.Join(",", list.Skip(i).Take(MAX_IN_SQL_PARAMETERS_COUNT)))
                     .Append(")")
                     .AppendLine(";");
                }

                return new Tuple<string, Dictionary<string, ParameterValue>>(sb.ToString(), parameters);
            }

            protected Tuple<string, Dictionary<string, ParameterValue>> Complex(string tableName, Dictionary<string, string> cols, IEnumerable<TEntity> entities)
            {
                var sb = new StringBuilder();

                var parameters = new Dictionary<string, ParameterValue>(entities.Count() * cols.Count);

                sb.Append("DELETE FROM ")
                   .Append(Settings.Name(tableName))
                   .Append(" WHERE ")
                   .Append(string.Join(" OR ", entities.Select((item, index) =>
                   {
                       var context = new ValidationContext(item, null, null);

                       return string.Concat("(", string.Join(" AND ", cols.Select(kv =>
                       {
                           var storeItem = typeItem.PropertyStores.First(x => x.Name == kv.Key);

                           var value = storeItem.Member.GetValue(item, null);

                           context.MemberName = storeItem.Member.Name;

                           var attrs = storeItem.Attributes.OfType<ValidationAttribute>();

                           if (attrs.Any())
                           {
                               ValidateValue(value, context, attrs);

                               value = storeItem.Member.GetValue(item, null);
                           }

                           var parameterKey = index == 0
                           ? Settings.ParamterName(kv.Key.ToUrlCase())
                           : Settings.ParamterName($"{kv.Key.ToUrlCase()}_{index}");

                           parameters.Add(parameterKey, ParameterValue.Create(value, storeItem.MemberType));

                           return string.Concat(Settings.Name(kv.Value), "=", parameterKey);
                       })), ")");
                   })));

                return new Tuple<string, Dictionary<string, ParameterValue>>(sb.ToString(), parameters);
            }

            public IDeleteableByCommit<TEntity> WatchSql(Action<CommandSql> watchSql)
            {
                SetWatchSql(watchSql);

                return this;
            }

            public IDeleteableByWhere<TEntity> SkipIdempotentValid()
            {
                foreach (var key in tableInfo.Tokens.Keys)
                {
                    whereColumns.Remove(key);
                }

                return this;
            }

        }

        private class DeleteableByAnalyseFrom : Deleteable
        {
            private readonly Func<ITableInfo, TEntity, string> tableGetter;

            public DeleteableByAnalyseFrom(Deleteable deleteable, Func<ITableInfo, TEntity, string> tableGetter) : base(deleteable)
            {
                this.tableGetter = tableGetter ?? throw new ArgumentNullException(nameof(tableGetter));
            }

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand(ICollection<TEntity> entries)
            {
                bool singleFlag = whereColumns.Count == 1;

                var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(1);

                entries.GroupBy(x => tableGetter.Invoke(tableInfo, x) ?? throw new DSyntaxErrorException("请指定表名称!"))
                    .ForEach(x =>
                    {
                        int parameterCount = x.Count() * whereColumns.Count;

                        if (parameterCount <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                        {
                            if (singleFlag)
                            {
                                results.Add(Simple(x.Key, whereColumns.First(), x));
                            }
                            else
                            {
                                results.Add(Complex(x.Key, whereColumns, x));
                            }
                        }
                        else
                        {
                            int offset = MAX_PARAMETERS_COUNT / whereColumns.Count;

                            for (int i = 0; i < entries.Count; i += offset)
                            {
                                if (singleFlag)
                                {
                                    results.Add(Simple(x.Key, whereColumns.First(), x.Skip(i).Take(offset)));
                                }
                                else
                                {
                                    results.Add(Complex(x.Key, whereColumns, x.Skip(i).Take(offset)));
                                }
                            }
                        }
                    });

                return results;
            }
        }

        private class DeleteableByFrom : Deleteable
        {
            private readonly Func<ITableInfo, string> tableGetter;

            public DeleteableByFrom(Deleteable deleteable, Func<ITableInfo, string> tableGetter) : base(deleteable)
            {
                this.tableGetter = tableGetter ?? throw new ArgumentNullException(nameof(tableGetter));
            }

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand(ICollection<TEntity> entries)
            {
                string tableName = tableGetter.Invoke(tableInfo) ?? throw new DSyntaxErrorException("请指定表名称!");

                bool singleFlag = whereColumns.Count == 1;

                int parameterCount = entries.Count * whereColumns.Count;

                List<Tuple<string, Dictionary<string, ParameterValue>>> results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(1);

                if (parameterCount <= MAX_PARAMETERS_COUNT)
                {
                    if (singleFlag)
                    {
                        results.Add(Simple(tableName, whereColumns.First(), entries));
                    }
                    else
                    {
                        results.Add(Complex(tableName, whereColumns, entries));
                    }
                }
                else
                {
                    int offset = MAX_PARAMETERS_COUNT / whereColumns.Count;

                    for (int i = 0; i < entries.Count; i += offset)
                    {
                        if (singleFlag)
                        {
                            results.Add(Simple(tableName, whereColumns.First(), entries.Skip(i).Take(offset)));
                        }
                        else
                        {
                            results.Add(Complex(tableName, whereColumns, entries.Skip(i).Take(offset)));
                        }
                    }
                }

                return results;
            }
        }
        #endregion

        #region Update
        private class Updateable : DbRouteExecuter, IUpdateable<TEntity>
        {
            private static readonly SortedDictionary<string, string> updates;
            private static readonly SortedDictionary<string, string> updateSets;

            static Updateable()
            {
                updates = new SortedDictionary<string, string>(MyStringComparer.Instance);
                updateSets = new SortedDictionary<string, string>(MyStringComparer.Instance);

                foreach (var key in tableInfo.Keys)
                {
                    updates.Add(key, tableInfo.ReadOrWrites[key]);
                }

                foreach (var kv in tableInfo.ReadWrites)
                {
                    updateSets.Add(kv.Key, kv.Value);
                }
            }

            protected readonly Dictionary<string, string> whereColumns;
            protected readonly Dictionary<string, string> updateSetColumns;

            protected Updateable(Updateable updateable) : base(updateable)
            {
                whereColumns = updateable.whereColumns;
                updateSetColumns = updateable.updateSetColumns;
            }

            public Updateable(IDbConnectionAdapter connectionAdapter, string connectionString, ICollection<TEntity> entries) : base(connectionAdapter, connectionString, entries)
            {
                whereColumns = new Dictionary<string, string>(updates);
                updateSetColumns = new Dictionary<string, string>(updateSets);

            }

            private void Aw_Except(string[] columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                var keys = new List<string>(columns.Length);

                foreach (var kv in updateSetColumns.Where(x => columns.Contains(x.Key) || columns.Contains(x.Value)))
                {
                    keys.Add(kv.Key);
                }

                if (updateSetColumns.Count == keys.Count)
                {
                    throw new DException("未指定更新字段!");
                }

                for (int i = 0, length = keys.Count; i < length; i++)
                {
                    if (tableInfo.Tokens.ContainsKey(keys[i]))
                    {
                        continue;
                    }

                    updateSetColumns.Remove(keys[i]);
                }
            }

            private void Aw_Limit(string[] columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                var keys = new List<string>(columns.Length);

                foreach (var kv in updateSetColumns.Where(x => !columns.Contains(x.Key) && !columns.Contains(x.Value)))
                {
                    keys.Add(kv.Key);
                }

                if (updateSetColumns.Count == keys.Count)
                {
                    throw new DException("未指定更新字段!");
                }

                for (int i = 0, length = keys.Count; i < length; i++)
                {
                    if (tableInfo.Tokens.ContainsKey(keys[i]))
                    {
                        continue;
                    }

                    updateSetColumns.Remove(keys[i]);
                }
            }

            private void Aw_Where(string[] columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                for (int i = 0, length = columns.Length; i < length; i++)
                {
                    bool flag = true;

                    string key = columns[i];

                    foreach (var kv in tableInfo.ReadOrWrites)
                    {
                        if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase) || string.Equals(kv.Value, key, StringComparison.OrdinalIgnoreCase))
                        {
                            whereColumns[kv.Key] = kv.Value;

                            flag = false;

                            break;
                        }
                    }

                    if (flag)
                    {
                        throw new DSyntaxErrorException($"未找到字段({key})!");
                    }
                }
            }

            public IUpdateableByLimit<TEntity> SetExcept(string[] columns)
            {
                Aw_Except(columns);

                return this;
            }

            public IUpdateableByLimit<TEntity> SetExcept<TColumn>(Expression<Func<TEntity, TColumn>> columns)
                => SetExcept(DbRouter.Except(columns));

            public IUpdateableByFrom<TEntity> Table(Func<ITableInfo, string> tableGetter)
                => new UpdateableByFrom(this, tableGetter);

            public IUpdateableByFrom<TEntity> Table(Func<ITableInfo, TEntity, string> tableGetter)
                => new UpdateableByAnalyseFrom(this, tableGetter);

            public IUpdateableByLimit<TEntity> Set(string[] columns)
            {
                Aw_Limit(columns);

                return this;
            }

            public IUpdateableByLimit<TEntity> Set<TColumn>(Expression<Func<TEntity, TColumn>> columns)
                => Set(DbRouter.Limit(columns));

            public IUpdateableByWhere<TEntity> Where(string[] columns)
            {
                Aw_Where(columns);

                return this;
            }

            public IUpdateableByWhere<TEntity> Where<TColumn>(Expression<Func<TEntity, TColumn>> columns)
                => Where(DbRouter.Where(columns));

            protected Tuple<string, Dictionary<string, ParameterValue>> SqlGenerator(string tableName, IEnumerable<TEntity> entities, Dictionary<string, string> updateSetColumns, Dictionary<string, string> whereColumns)
            {
                int count = entities.Count();

                var sb = new StringBuilder(count * 7);
                var parameters = new Dictionary<string, ParameterValue>(count * (updateSetColumns.Count + whereColumns.Count));

                entities.ForEach((entry, index) =>
                {
                    var context = new ValidationContext(entry, null, null);

                    string whereStr = string.Join(" AND ", whereColumns.Select(kv =>
                    {
                        string parameterKey = index == 0 ?
                            kv.Key.ToUrlCase()
                            :
                            $"{kv.Key.ToUrlCase()}_{index}";

                        var storeItem = typeItem.PropertyStores.First(x => x.Name == kv.Key);

                        var value = storeItem.Member.GetValue(entry, null);

                        context.MemberName = storeItem.Member.Name;

                        var attrs = storeItem.Attributes.OfType<ValidationAttribute>();

                        if (attrs.Any())
                        {
                            ValidateValue(value, context, attrs);
                        }

                        parameters.Add(parameterKey, ParameterValue.Create(value, storeItem.MemberType));

                        return string.Concat(Settings.Name(kv.Value), "=", Settings.ParamterName(parameterKey));
                    }));

                    sb.Append("UPDATE ")
                        .Append(Settings.Name(tableName))
                        .Append(" SET ")
                        .Append(string.Join(",", updateSetColumns.Select(kv =>
                        {
                            object value;

                            var storeItem = typeItem.PropertyStores.First(x => x.Name == kv.Key);

                            context.MemberName = storeItem.Member.Name;

                            if (tableInfo.Tokens.TryGetValue(kv.Key, out TokenAttribute token))
                            {
                                value = token.Create();

                                if (value is null)
                                {
                                    throw new NoNullAllowedException("令牌不允许为空!");
                                }

                                storeItem.Member.SetValue(entry, value, null);
                            }
                            else
                            {
                                value = storeItem.Member.GetValue(entry, null);
                            }

                            var attrs = storeItem.Attributes.OfType<ValidationAttribute>();

                            if (attrs.Any())
                            {
                                ValidateValue(value, context, attrs);
                            }

                            var name = tableInfo.Tokens.ContainsKey(kv.Key)
                            ? $"__token_{kv.Value}"
                            : kv.Value;

                            string parameterKey = index == 0
                            ? name
                            :
                            $"{name}_{index}";

                            parameters.Add(parameterKey, ParameterValue.Create(value, storeItem.MemberType));

                            return string.Concat(Settings.Name(kv.Value), "=", Settings.ParamterName(parameterKey));

                        })))
                        .Append(" WHERE ")
                        .Append(whereStr)
                        .Append(";");
                });

                return new Tuple<string, Dictionary<string, ParameterValue>>(sb.ToString(), parameters);
            }

            public override bool IsValid() => whereColumns.Count > 0 && updateSetColumns.Count > 0;

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand(ICollection<TEntity> entries)
            {
                string tableName = tableInfo.TableName;

                int parameterCount = entries.Count * (updateSetColumns.Count + tableInfo.Tokens.Count + whereColumns.Count);

                var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(parameterCount / MAX_PARAMETERS_COUNT + 1);

                if (parameterCount <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                {
                    results.Add(SqlGenerator(tableName, entries, updateSetColumns, whereColumns));
                }
                else
                {
                    int offset = MAX_PARAMETERS_COUNT / (updateSetColumns.Count + tableInfo.Tokens.Count + whereColumns.Count);

                    for (int i = 0; i < entries.Count; i += offset)
                    {
                        results.Add(SqlGenerator(tableName, entries.Skip(i).Take(offset), updateSetColumns, whereColumns));
                    }
                }

                return results;
            }

            public IUpdateableByCommit<TEntity> WatchSql(Action<CommandSql> watchSql)
            {
                SetWatchSql(watchSql);

                return this;
            }

            public IUpdateableByWhere<TEntity> SkipIdempotentValid()
            {
                foreach (var key in tableInfo.Tokens.Keys)
                {
                    whereColumns.Remove(key);
                }

                return this;
            }

            public IUpdateableByCommit<TEntity> Transaction()
            {
                SetTransaction(IsolationLevel.ReadCommitted);

                return this;
            }

            public IUpdateableByCommit<TEntity> Transaction(IsolationLevel isolationLevel)
            {
                SetTransaction(isolationLevel);

                return this;
            }
        }

        private class UpdateableByFrom : Updateable
        {
            private readonly Func<ITableInfo, string> tableGetter;

            public UpdateableByFrom(Updateable updateable, Func<ITableInfo, string> tableGetter) : base(updateable)
            {
                this.tableGetter = tableGetter ?? throw new ArgumentNullException(nameof(tableGetter));
            }

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand(ICollection<TEntity> entries)
            {
                string tableName = tableGetter.Invoke(tableInfo) ?? throw new DSyntaxErrorException();


                int parameterCount = entries.Count * (updateSetColumns.Count + tableInfo.Tokens.Count + whereColumns.Count);

                if (parameterCount <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                {
                    var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(1)
                    {
                        SqlGenerator(tableName, entries, updateSetColumns, whereColumns)
                    };

                    return results;
                }
                else
                {
                    int offset = MAX_PARAMETERS_COUNT / (updateSetColumns.Count + tableInfo.Tokens.Count + whereColumns.Count);

                    var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(entries.Count / offset + 1);

                    for (int i = 0; i < entries.Count; i += offset)
                    {
                        results.Add(SqlGenerator(tableName, entries.Skip(i).Take(offset), updateSetColumns, whereColumns));
                    }
                    return results;
                }

            }
        }

        private class UpdateableByAnalyseFrom : Updateable
        {
            private readonly Func<ITableInfo, TEntity, string> tableGetter;

            public UpdateableByAnalyseFrom(Updateable updateable, Func<ITableInfo, TEntity, string> tableGetter) : base(updateable)
            {
                this.tableGetter = tableGetter ?? throw new ArgumentNullException(nameof(tableGetter));
            }

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand(ICollection<TEntity> entries)
            {
                int total = entries.Count * (updateSetColumns.Count + tableInfo.Tokens.Count + whereColumns.Count);

                var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>(total / MAX_PARAMETERS_COUNT + 1);

                entries.GroupBy(x => tableGetter.Invoke(tableInfo, x) ?? throw new DSyntaxErrorException("请指定表名称!"))
                    .ForEach(x =>
                    {
                        int parameterCount = x.Count() * (updateSetColumns.Count + tableInfo.Tokens.Count + whereColumns.Count);

                        if (parameterCount <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                        {
                            results.Add(SqlGenerator(x.Key, entries, updateSetColumns, whereColumns));
                        }
                        else
                        {
                            int offset = MAX_PARAMETERS_COUNT / (updateSetColumns.Count + tableInfo.Tokens.Count + whereColumns.Count);

                            for (int i = 0; i < entries.Count; i += offset)
                            {
                                results.Add(SqlGenerator(x.Key, x.Skip(i).Take(offset), updateSetColumns, whereColumns));
                            }
                        }
                    });

                return results;
            }
        }
        #endregion

        /// <summary>
        /// 赋予插入能力。
        /// </summary>
        /// <param name="entry">实体。</param>
        /// <returns></returns>
        public IInsertable<TEntity> AsInsertable(TEntity entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return new Insertable(connectionAdapter, connectionString, new TEntity[] { entry });
        }

        /// <summary>
        /// 赋予插入能力。
        /// </summary>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public IInsertable<TEntity> AsInsertable(TEntity[] entries)
            => new Insertable(connectionAdapter, connectionString, entries);

        /// <summary>
        /// 赋予插入能力。
        /// </summary>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public IInsertable<TEntity> AsInsertable(List<TEntity> entries)
            => new Insertable(connectionAdapter, connectionString, entries);

        /// <summary>
        /// 赋予更新能力。
        /// </summary>
        /// <param name="entry">集合。</param>
        /// <returns></returns>
        public IUpdateable<TEntity> AsUpdateable(TEntity entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return new Updateable(connectionAdapter, connectionString, new TEntity[] { entry });
        }

        /// <summary>
        /// 赋予更新能力。
        /// </summary>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public IUpdateable<TEntity> AsUpdateable(TEntity[] entries)
            => new Updateable(connectionAdapter, connectionString, entries);

        /// <summary>
        /// 赋予更新能力。
        /// </summary>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public IUpdateable<TEntity> AsUpdateable(List<TEntity> entries)
            => new Updateable(connectionAdapter, connectionString, entries);

        /// <summary>
        /// 赋予删除能力。
        /// </summary>
        /// <param name="entry">实体。</param>
        /// <returns></returns>
        public IDeleteable<TEntity> AsDeleteable(TEntity entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return new Deleteable(connectionAdapter, connectionString, new TEntity[] { entry });
        }

        /// <summary>
        /// 赋予删除能力。
        /// </summary>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public IDeleteable<TEntity> AsDeleteable(TEntity[] entries)
            => new Deleteable(connectionAdapter, connectionString, entries);

        /// <summary>
        /// 赋予删除能力。
        /// </summary>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public IDeleteable<TEntity> AsDeleteable(List<TEntity> entries)
            => new Deleteable(connectionAdapter, connectionString, entries);
    }
}

using CodeArts.Db.Exceptions;
using CodeArts.Runtime;
using Dapper;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据写入器。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public class DbWriter<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// 实体表信息。
        /// </summary>
        public static ITableInfo TableInfo { get; }

        static DbWriter() => TableInfo = TableRegions.Resolve<TEntity>();

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
        private abstract class DbRouteExecuter : IDbRouteExecuter<TEntity>
        {
            private readonly IDatabaseFor databaseFor;
            public DbRouteExecuter(IDatabase database, ICollection<TEntity> entries)
            {
                this.Entries = entries ?? throw new ArgumentNullException(nameof(entries));
                this.database = database ?? throw new ArgumentNullException(nameof(database));
                this.databaseFor = DbConnectionManager.GetOrCreate(DbConnectionManager.Get(database.ProviderName));
            }

            static DbRouteExecuter()
            {
                typeStore = TypeItem.Get<TEntity>();
                typeRegions = TableRegions.Resolve<TEntity>();
                defaultLimit = typeRegions.ReadWrites.Keys.ToArray();
                defaultWhere = typeRegions.Keys.ToArray();
            }

            protected static readonly TypeItem typeStore;
            protected static readonly ITableInfo typeRegions;
            protected static readonly string[] defaultLimit;
            protected static readonly string[] defaultWhere;

            private readonly IDatabase database;
            private IsolationLevel? isolationLevel = null;
            protected static readonly ConcurrentDictionary<Type, object> DefaultCache = new ConcurrentDictionary<Type, object>();

            public IDbRouteExecuter<TEntity> UseTransaction()
            {
                isolationLevel = IsolationLevel.ReadCommitted;

                return this;
            }

            public IDbRouteExecuter<TEntity> UseTransaction(IsolationLevel isolationLevel)
            {
                this.isolationLevel = isolationLevel;

                return this;
            }

            public IDbRouter<TEntity> DbRouter => DbRouter<TEntity>.Instance;

            public ICollection<TEntity> Entries { get; }

            public ISQLCorrectSettings Settings => database.Settings;

            public int ExecuteCommand(int? commandTimeout = null)
            {
                if (Entries.Count == 0)
                {
                    return 0;
                }

                var results = PrepareCommand();

                if (results.Count == 0)
                {
                    throw new NotSupportedException();
                }

                if (isolationLevel.HasValue)
                {
                    return UseTransactionExecute(results, commandTimeout);
                }

                return Execute(results, commandTimeout);
            }

            private int Execute(List<Tuple<string, Dictionary<string, ParameterValue>>> results, int? commandTimeout)
            {
                int influenceLine = 0;

                SqlCapture capture = SqlCapture.Current;

                var isCloseConnection = database.State == ConnectionState.Closed;

                if (isCloseConnection)
                {
                    database.Open();
                }

                try
                {
                    if (commandTimeout.HasValue)
                    {
                        Stopwatch stopwatch = new Stopwatch();

                        results.ForEach(x =>
                        {
                            int? timeOut = commandTimeout - (int)(stopwatch.ElapsedMilliseconds / 1000L);

                            var commandSql = new CommandSql(x.Item1, x.Item2, timeOut);

                            capture?.Capture(commandSql);

                            stopwatch.Start();

                            influenceLine += databaseFor.Execute(database, commandSql);

                            stopwatch.Stop();
                        });
                    }
                    else
                    {
                        results.ForEach(x =>
                        {
                            var commandSql = new CommandSql(x.Item1, x.Item2, commandTimeout);

                            capture?.Capture(commandSql);

                            influenceLine += databaseFor.Execute(database, commandSql);
                        });
                    }

                    return influenceLine;
                }
                finally
                {
                    if (isCloseConnection)
                    {
                        database.Close();
                    }
                }
            }

            private int UseTransactionExecute(List<Tuple<string, Dictionary<string, ParameterValue>>> results, int? commandTimeout)
            {
                int influenceLine = 0;

                SqlCapture capture = SqlCapture.Current;

                var isCloseConnection = database.State == ConnectionState.Closed;

                if (isCloseConnection)
                {
                    database.Open();
                }

                try
                {
                    using (var transaction = database.BeginTransaction(isolationLevel.Value))
                    {
                        try
                        {
                            if (commandTimeout.HasValue)
                            {
                                Stopwatch stopwatch = new Stopwatch();

                                results.ForEach(x =>
                                {
                                    int? timeOut = commandTimeout - (int)(stopwatch.ElapsedMilliseconds / 1000L);

                                    var commandSql = new CommandSql(x.Item1, x.Item2, timeOut);

                                    capture?.Capture(commandSql);

                                    stopwatch.Start();

                                    influenceLine += databaseFor.Execute(database, commandSql);

                                    stopwatch.Stop();
                                });
                            }
                            else
                            {
                                results.ForEach(x =>
                                {
                                    var commandSql = new CommandSql(x.Item1, x.Item2, commandTimeout);

                                    influenceLine += databaseFor.Execute(database, commandSql);
                                });
                            }

                            transaction.Commit();

                            return influenceLine;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();

                            throw;
                        }
                    }
                }
                finally
                {
                    if (isCloseConnection)
                    {
                        database.Close();
                    }
                }
            }

            public abstract List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand();

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
            public Task<int> ExecuteCommandAsync(CancellationToken cancellationToken = default) => ExecuteCommandAsync(null, cancellationToken);

            public Task<int> ExecuteCommandAsync(int? commandTimeout, CancellationToken cancellationToken = default)
            {
                if (Entries.Count == 0)
                {
                    return Task.FromResult(0);
                }

                var results = PrepareCommand();

                if (results.Count == 0)
                {
                    throw new NotSupportedException();
                }

                if (isolationLevel.HasValue)
                {
                    return UseTransactionAsync(results, commandTimeout, cancellationToken);
                }

                return ExecutedAsync(results, commandTimeout, cancellationToken);
            }

            private async Task<int> ExecutedAsync(List<Tuple<string, Dictionary<string, ParameterValue>>> results, int? commandTimeout, CancellationToken cancellationToken = default)
            {
                int influenceLine = 0;

                SqlCapture capture = SqlCapture.Current;

                var isCloseConnection = database.State == ConnectionState.Closed;

                if (isCloseConnection)
                {
                    if (database is System.Data.Common.DbConnection connection)
                    {
                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        database.Open();
                    }
                }

                try
                {
                    if (commandTimeout.HasValue)
                    {
                        Stopwatch stopwatch = new Stopwatch();

                        foreach (var x in results)
                        {
                            int? timeOut = commandTimeout - (int)(stopwatch.ElapsedMilliseconds / 1000L);

                            var commandSql = new CommandSql(x.Item1, x.Item2, timeOut);

                            capture?.Capture(commandSql);

                            stopwatch.Start();

                            influenceLine += await databaseFor.ExecuteAsync(database, commandSql);

                            stopwatch.Stop();
                        }
                    }
                    else
                    {
                        foreach (var x in results)
                        {
                            var commandSql = new CommandSql(x.Item1, x.Item2, commandTimeout);

                            capture?.Capture(commandSql);

                            influenceLine += await databaseFor.ExecuteAsync(database, commandSql);
                        }
                    }

                    return influenceLine;
                }
                finally
                {
                    if (isCloseConnection)
                    {
#if NETSTANDARD2_1_OR_GREATER
                        if (database is System.Data.Common.DbConnection connection)
                        {
                            await connection.CloseAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            database.Close();
                        }
#else
                        database.Close();
#endif
                    }
                }
            }

            private async Task<int> UseTransactionAsync(List<Tuple<string, Dictionary<string, ParameterValue>>> results, int? commandTimeout, CancellationToken cancellationToken = default)
            {
                int influenceLine = 0;

                SqlCapture capture = SqlCapture.Current;

                var isCloseConnection = database.State == ConnectionState.Closed;

                if (database is System.Data.Common.DbConnection connection)
                {
                    if (isCloseConnection)
                    {
                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    }

                    try
                    {
#if NETSTANDARD2_1_OR_GREATER
                        await using (var transaction = await connection.BeginTransactionAsync(isolationLevel.Value, cancellationToken).ConfigureAwait(false))
#else
                        using (var transaction = connection.BeginTransaction(isolationLevel.Value))
#endif
                        {
                            try
                            {
                                if (commandTimeout.HasValue)
                                {
                                    Stopwatch stopwatch = new Stopwatch();

                                    foreach (var x in results)
                                    {
                                        int? timeOut = commandTimeout - (int)(stopwatch.ElapsedMilliseconds / 1000L);

                                        var commandSql = new CommandSql(x.Item1, x.Item2, timeOut);

                                        capture?.Capture(commandSql);

                                        stopwatch.Start();

                                        influenceLine += await databaseFor.ExecuteAsync(connection, commandSql);

                                        stopwatch.Stop();
                                    }
                                }
                                else
                                {
                                    foreach (var x in results)
                                    {
                                        var commandSql = new CommandSql(x.Item1, x.Item2, commandTimeout);

                                        capture?.Capture(commandSql);

                                        influenceLine += await databaseFor.ExecuteAsync(connection, commandSql);
                                    }
                                }

#if NETSTANDARD2_1_OR_GREATER
                                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
#else
                                transaction.Commit();
#endif
                            }
                            catch (Exception)
                            {
#if NETSTANDARD2_1_OR_GREATER
                                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
#else
                                transaction.Rollback();
#endif

                                throw;
                            }
                        }

                        return influenceLine;
                    }
                    finally
                    {
#if NETSTANDARD2_1_OR_GREATER
                        await connection.CloseAsync().ConfigureAwait(false);
#else
                        connection.Close();
#endif
                    }
                }

                if (isCloseConnection)
                {
                    database.Open();
                }

                try
                {
                    using (var transaction = database.BeginTransaction(isolationLevel.Value))
                    {
                        try
                        {
                            if (commandTimeout.HasValue)
                            {
                                Stopwatch stopwatch = new Stopwatch();

                                foreach (var x in results)
                                {
                                    int? timeOut = commandTimeout - (int)(stopwatch.ElapsedMilliseconds / 1000L);

                                    var commandSql = new CommandSql(x.Item1, x.Item2, timeOut);

                                    capture?.Capture(commandSql);

                                    stopwatch.Start();

                                    influenceLine += databaseFor.Execute(database, commandSql);

                                    stopwatch.Stop();
                                }
                            }
                            else
                            {
                                foreach (var x in results)
                                {
                                    var commandSql = new CommandSql(x.Item1, x.Item2, commandTimeout);

                                    capture?.Capture(commandSql);

                                    influenceLine += databaseFor.Execute(database, commandSql);
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();

                            throw;
                        }
                    }

                    return influenceLine;
                }
                finally
                {
                    database.Close();
                }
            }
#endif
        }

        private class StringArrayComparer : IEqualityComparer<string[]>
        {
            public bool Equals(string[] x, string[] y)
            {
                if (x.Length != y.Length)
                {
                    return false;
                }

                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(string[] obj)
            {
                int hashCode = 0;

                foreach (var item in obj)
                {
                    hashCode += item.GetHashCode();
                }

                return hashCode;
            }
        }

        private class Deleteable : DbRouteExecuter, IDeleteable<TEntity>
        {
            public Deleteable(IDatabase context, ICollection<TEntity> entries) : base(context, entries)
            {
                wheres = defaultWhere;
            }

            private string[] wheres;
            private Func<TEntity, string[]> where;
            private Func<ITableInfo, string> from;
            private List<Tuple<string, Dictionary<string, ParameterValue>>> Simple()
            {
                bool flag = true;
                string value = wheres[0];
                KeyValuePair<string, string> column = default;

                foreach (var kv in typeRegions.ReadOrWrites)
                {
                    if (string.Equals(kv.Key, value, StringComparison.OrdinalIgnoreCase) || string.Equals(kv.Value, value, StringComparison.OrdinalIgnoreCase))
                    {
                        flag = false;

                        column = kv;

                        break;
                    }
                }

                if (flag)
                {
                    throw new DException("未指定删除条件!");
                }

                List<Tuple<string, Dictionary<string, ParameterValue>>> results = new List<Tuple<string, Dictionary<string, ParameterValue>>>();

                if (Entries.Count <= MAX_PARAMETERS_COUNT)
                {
                    results.Add(Simple(column, Entries));
                }
                else
                {
                    for (int i = 0; i < Entries.Count; i += MAX_PARAMETERS_COUNT)
                    {
                        results.Add(Simple(column, Entries.Skip(i).Take(MAX_PARAMETERS_COUNT)));
                    }
                }

                return results;
            }

            private List<Tuple<string, Dictionary<string, ParameterValue>>> Complex()
            {
                List<Tuple<string, Dictionary<string, ParameterValue>>> results = new List<Tuple<string, Dictionary<string, ParameterValue>>>();

                var columns = typeRegions.ReadOrWrites
                        .Where(x => wheres.Any(y => string.Equals(y, x.Key, StringComparison.OrdinalIgnoreCase)) || wheres.Any(y => string.Equals(y, x.Value, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                if (Entries.Count * columns.Count <= MAX_PARAMETERS_COUNT)
                {
                    results.Add(Complex(columns, Entries));

                    return results;
                }

                int offset = MAX_PARAMETERS_COUNT / columns.Count;

                var sqls = new List<KeyValuePair<string, Dictionary<string, object>>>();

                for (int i = 0; i < Entries.Count; i += offset)
                {
                    results.Add(Complex(columns, Entries.Skip(i).Take(offset)));
                }

                return results;
            }

            private Tuple<string, Dictionary<string, ParameterValue>> Simple(KeyValuePair<string, string> column, IEnumerable<TEntity> collect)
            {
                var sb = new StringBuilder();

                string name = column.Key.ToUrlCase();
                string tableName = Settings.Name(from?.Invoke(typeRegions) ?? typeRegions.TableName);

                var storeItem = typeStore.PropertyStores.First(x => x.Name == column.Key);

                sb.Append("DELETE FROM ")
                .Append(tableName)
                .Append(" WHERE ")
                .Append(column.Value);

                Dictionary<string, ParameterValue> parameters = new Dictionary<string, ParameterValue>();

                var list = collect.Select((item, index) =>
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
                            .Append(tableName)
                            .Append(" WHERE ")
                            .Append(column.Value);
                    }

                    sb.Append(" IN (")
                     .Append(string.Join(",", list.Skip(i).Take(MAX_IN_SQL_PARAMETERS_COUNT)))
                     .Append(")")
                     .AppendLine(";");
                }

                return new Tuple<string, Dictionary<string, ParameterValue>>(sb.ToString(), parameters);
            }

            private Tuple<string, Dictionary<string, ParameterValue>> Complex(List<KeyValuePair<string, string>> columns, IEnumerable<TEntity> collect)
            {
                var sb = new StringBuilder();

                var parameters = new Dictionary<string, ParameterValue>();

                sb.Append("DELETE FROM ")
                   .Append(Settings.Name(from?.Invoke(typeRegions) ?? typeRegions.TableName))
                   .Append(" WHERE ")
                   .Append(string.Join(" OR ", collect.Select((item, index) =>
                   {
                       var context = new ValidationContext(item, null, null);

                       return string.Concat("(", string.Join(" AND ", columns.Select(kv =>
                       {
                           var storeItem = typeStore.PropertyStores.First(x => x.Name == kv.Key);

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

            private string Simple(KeyValuePair<string, string> column, IEnumerable<TEntity> collect, int groupIndex, Dictionary<string, ParameterValue> parameters)
            {
                var sb = new StringBuilder();

                string name = column.Key.ToUrlCase();
                string tableName = Settings.Name(from?.Invoke(typeRegions) ?? typeRegions.TableName);

                var storeItem = typeStore.PropertyStores.First(x => x.Name == column.Key);

                sb.Append("DELETE FROM ")
                .Append(tableName)
                .Append(" WHERE ")
                .Append(column.Value);

                var list = collect.Select((item, index) =>
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

                     var parameterKey = index == 0 && groupIndex == 0 ?
                         name
                         :
                         $"{name}_{groupIndex}_{index}";

                     parameters.Add(parameterKey, ParameterValue.Create(value, storeItem.MemberType));

                     return Settings.ParamterName(parameterKey);

                 }).ToList();

                if (list.Count == 1)
                {
                    return sb.Append("=")
                        .Append(list[0])
                        .ToString();
                }

                if (list.Count <= MAX_IN_SQL_PARAMETERS_COUNT)
                {
                    return sb.Append(" IN (")
                     .Append(string.Join(",", list))
                     .Append(")")
                     .ToString();
                }

                for (int i = 0; i < list.Count; i += MAX_IN_SQL_PARAMETERS_COUNT)
                {
                    if (i > 0)
                    {
                        sb.Append("DELETE FROM ")
                            .Append(tableName)
                            .Append(" WHERE ")
                            .Append(column.Value);
                    }

                    sb.Append(" IN (")
                     .Append(string.Join(",", list.Skip(i).Take(MAX_IN_SQL_PARAMETERS_COUNT)))
                     .Append(")")
                     .AppendLine(";");
                }

                return sb.ToString();
            }

            private string Complex(List<KeyValuePair<string, string>> columns, IEnumerable<TEntity> collect, int groupIndex, Dictionary<string, ParameterValue> parameters)
            {
                var sb = new StringBuilder();

                sb.Append("DELETE FROM ")
                   .Append(Settings.Name(from?.Invoke(typeRegions) ?? typeRegions.TableName))
                   .Append(" WHERE ")
                   .Append(string.Join(" OR ", collect.Select((item, index) =>
                   {
                       var context = new ValidationContext(item, null, null);

                       return string.Concat("(", string.Join(" AND ", columns.Select(kv =>
                       {
                           var storeItem = typeStore.PropertyStores.First(x => x.Name == kv.Key);

                           var value = storeItem.Member.GetValue(item, null);

                           context.MemberName = storeItem.Member.Name;

                           var attrs = storeItem.Attributes.OfType<ValidationAttribute>();

                           if (attrs.Any())
                           {
                               ValidateValue(value, context, attrs);

                               value = storeItem.Member.GetValue(item, null);
                           }

                           string key = kv.Key.ToUrlCase();

                           var parameterKey = index == 0 && groupIndex == 0
                           ? Settings.ParamterName(key)
                           : Settings.ParamterName(groupIndex == 0 ? $"{key}_{index}" : $"{key}_{groupIndex}_{index}");

                           parameters.Add(parameterKey, ParameterValue.Create(value, storeItem.MemberType));

                           return string.Concat(Settings.Name(kv.Value), "=", parameterKey);
                       })), ")");
                   })));

                return sb.ToString();
            }

            public IDeleteable<TEntity> From(Func<ITableInfo, string> table)
            {
                from = table;

                return this;
            }

            public IDeleteable<TEntity> Where(string[] columns)
            {
                wheres = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IDeleteable<TEntity> Where<TColumn>(Expression<Func<TEntity, TColumn>> columns)
            {
                where = DbRouter.Where(columns);

                return this;
            }

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand()
            {
                if (where is null)
                {
                    if (wheres.Length == 0)
                    {
                        throw new DException("未指定删除条件!");
                    }

                    return wheres.Length == 1 ? Simple() : Complex();
                }

                var dicRoot = new List<KeyValuePair<TEntity, string>>();

                int parameterCount = 0;

                var listRoot = Entries.GroupBy(item =>
                {
                    var wheres = where.Invoke(item);

                    if (wheres.Length == 0)
                    {
                        throw new DException("未指定删除条件!");
                    }

                    var columns = typeRegions.ReadOrWrites
                        .Where(x => typeRegions.Keys.Contains(x.Key) || wheres.Any(y => y == x.Key || y == x.Value))
                        .Select(x => x.Key)
                        .ToArray();

                    if (columns.Length == 0)
                    {
                        throw new DException("未指定删除条件!");
                    }

                    parameterCount += columns.Length;

                    return columns;

                }, Singleton<StringArrayComparer>.Instance)
                .ToList();

                var parameters = new Dictionary<string, ParameterValue>();

                List<Tuple<string, Dictionary<string, ParameterValue>>> results = new List<Tuple<string, Dictionary<string, ParameterValue>>>();

                if (parameterCount <= MAX_PARAMETERS_COUNT)
                {
                    string sql = string.Join(";", listRoot.Select((item, index) =>
                    {
                        if (item.Key.Length > 1)
                        {
                            return Complex(TableInfo.ReadWrites.Where(x => item.Key.Contains(x.Key)).ToList(), item, index, parameters);
                        }

                        string key = item.Key[0];

                        return Simple(TableInfo.ReadWrites.First(x => x.Key == key), item, index, parameters);
                    }));

                    results.Add(new Tuple<string, Dictionary<string, ParameterValue>>(sql, parameters));

                    return results;
                }

                bool flag = false;

                int groupIndex = 0;

                parameterCount = 0;

                StringBuilder sb = new StringBuilder();

                foreach (var item in listRoot.OrderBy(x => x.Key.Length))
                {
                    parameterCount += item.Key.Length;

                    if (parameterCount > MAX_PARAMETERS_COUNT)
                    {
                        results.Add(new Tuple<string, Dictionary<string, ParameterValue>>(sb.ToString(), parameters));

                        sb.Clear();

                        flag = false;

                        groupIndex = 0;

                        parameters = new Dictionary<string, ParameterValue>();

                        parameterCount = item.Key.Length;
                    }

                    if (flag)
                    {
                        sb.Append(';');
                    }
                    else
                    {
                        flag = true;
                    }

                    if (item.Key.Length == 1)
                    {
                        string key = item.Key[0];

                        sb.Append(Simple(TableInfo.ReadOrWrites.First(x => x.Key == key), item, groupIndex, parameters));
                    }
                    else
                    {
                        sb.Append(Complex(TableInfo.ReadOrWrites.Where(x => item.Key.Contains(x.Key)).ToList(), item, groupIndex, parameters));
                    }

                    groupIndex++;
                }

                if (sb.Length > 0)
                {
                    results.Add(new Tuple<string, Dictionary<string, ParameterValue>>(sb.ToString(), parameters));
                }

                return results;
            }
        }

        private class Insertable : DbRouteExecuter, IInsertable<TEntity>
        {
            public Insertable(IDatabase context, ICollection<TEntity> entries) : base(context, entries)
            {
            }

            private string[] limits;
            private string[] excepts;
            private Func<ITableInfo, string> from;

            public IInsertable<TEntity> Except(string[] columns)
            {
                excepts = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IInsertable<TEntity> Except<TColumn>(Expression<Func<TEntity, TColumn>> columns) => Except(DbRouter.Except(columns));

            private Tuple<string, Dictionary<string, ParameterValue>> SqlGenerator(IEnumerable<TEntity> list, List<KeyValuePair<string, string>> columns)
            {
                var sb = new StringBuilder();
                var parameters = new Dictionary<string, ParameterValue>();

                sb.Append("INSERT INTO ")
                    .Append(Settings.Name(from?.Invoke(typeRegions) ?? typeRegions.TableName))
                    .Append("(")
                    .Append(string.Join(",", columns.Select(x => Settings.Name(x.Value))))
                    .Append(")")
                    .Append(" VALUES ")
                    .Append(string.Join(",", list.Select((item, index) =>
                    {
                        var context = new ValidationContext(item, null, null);

                        return string.Concat("(", string.Join(",", columns.Select(kv =>
                        {
                            var storeItem = typeStore.PropertyStores.First(x => x.Name == kv.Key);

                            var parameterKey = index == 0 ?
                                kv.Key.ToUrlCase()
                                :
                                $"{kv.Key.ToUrlCase()}_{index}";

                            var value = storeItem.Member.GetValue(item, null);

                            if (value is null || storeItem.MemberType.IsValueType && Equals(value, DefaultCache.GetOrAdd(storeItem.MemberType, type => Activator.CreateInstance(type))))
                            {
                                if (typeRegions.Tokens.TryGetValue(kv.Key, out TokenAttribute token))
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

            public IInsertable<TEntity> From(Func<ITableInfo, string> table)
            {
                from = table ?? throw new ArgumentNullException(nameof(table));

                return this;
            }

            public IInsertable<TEntity> Limit(string[] columns)
            {
                limits = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IInsertable<TEntity> Limit<TColumn>(Expression<Func<TEntity, TColumn>> columns) => Limit(DbRouter.Limit(columns));

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand()
            {
                IEnumerable<KeyValuePair<string, string>> columns = typeRegions.ReadWrites;

                if (!(limits is null))
                {
                    columns = columns.Where(x => limits.Any(y => string.Equals(y, x.Key, StringComparison.OrdinalIgnoreCase) || string.Equals(y, x.Value, StringComparison.OrdinalIgnoreCase)));
                }

                if (!(excepts is null))
                {
                    columns = columns.Where(x => !excepts.Any(y => string.Equals(y, x.Key, StringComparison.OrdinalIgnoreCase) || string.Equals(y, x.Value, StringComparison.OrdinalIgnoreCase)));
                }

                if (!columns.Any())
                {
                    throw new DException("未指定插入字段!");
                }

                var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>();

                var insertColumns = columns.ToList();

                int parameterCount = Entries.Count * insertColumns.Count;

                if (parameterCount <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                {
                    results.Add(SqlGenerator(Entries, insertColumns));
                }
                else
                {
                    int offset = MAX_PARAMETERS_COUNT / insertColumns.Count;

                    for (int i = 0; i < Entries.Count; i += offset)
                    {
                        results.Add(SqlGenerator(Entries.Skip(i).Take(offset), insertColumns));
                    }
                }

                return results;
            }
        }

        private class KeyValueStringComparer : IEqualityComparer<KeyValuePair<string, string>>
        {
            public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                return x.Key == y.Key && x.Value == y.Value;
            }

            public int GetHashCode(KeyValuePair<string, string> obj)
            {
                return obj.Key.GetHashCode() + obj.Value.GetHashCode();
            }
        }

        private class Updateable : DbRouteExecuter, IUpdateable<TEntity>
        {
            public Updateable(IDatabase context, ICollection<TEntity> entries) : base(context, entries)
            {
            }

            private string[] limits;
            private string[] excepts;
            private Func<TEntity, string[]> where;
            private Func<ITableInfo, TEntity, string> from;

            private static readonly Dictionary<string, string> defalutUpdateFields = new Dictionary<string, string>();

            static Updateable()
            {
                foreach (var kv in typeRegions.ReadWrites)
                {
                    if (typeRegions.Keys.Contains(kv.Key))
                    {
                        continue;
                    }

                    defalutUpdateFields.Add(kv.Key, kv.Value);
                }
            }

            public IUpdateable<TEntity> Except(string[] columns)
            {
                excepts = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IUpdateable<TEntity> Except<TColumn>(Expression<Func<TEntity, TColumn>> columns) => Except(DbRouter.Except(columns));

            private Tuple<string, Dictionary<string, ParameterValue>> SqlGenerator(List<KeyValuePair<TEntity, string[]>> list, List<KeyValuePair<string, string>> columns)
            {
                var sb = new StringBuilder();
                var parameters = new Dictionary<string, ParameterValue>();

                list.ForEach((kvr, index) =>
                {
                    var entry = kvr.Key;

                    var wheres = kvr.Value;

                    var context = new ValidationContext(entry, null, null);

                    string whereStr = string.Join(" AND ", typeRegions.ReadOrWrites
                    .Where(x => wheres.Any(y => y == x.Key))
                    .Select(kv =>
                    {
                        string parameterKey = index == 0 ?
                            kv.Key.ToUrlCase()
                            :
                            $"{kv.Key.ToUrlCase()}_{index}";

                        var storeItem = typeStore.PropertyStores.First(x => x.Name == kv.Key);

                        var value = storeItem.Member.GetValue(entry, null);

                        context.MemberName = storeItem.Member.Name;

                        var attrs = storeItem.Attributes.OfType<ValidationAttribute>();

                        if (attrs.Any())
                        {
                            ValidateValue(value, context, attrs);
                        }

                        parameters.Add(parameterKey, ParameterValue.Create(value, storeItem.MemberType));

                        if (typeRegions.Tokens.TryGetValue(kv.Key, out TokenAttribute token))
                        {
                            value = token.Create();

                            if (value is null)
                            {
                                throw new NoNullAllowedException("令牌不允许为空!");
                            }

                            storeItem.Member.SetValue(entry, value, null);
                        }

                        return string.Concat(Settings.Name(kv.Value), "=", Settings.ParamterName(parameterKey));
                    }));

                    sb.Append("UPDATE ")
                        .Append(Settings.Name(from?.Invoke(typeRegions, entry) ?? typeRegions.TableName))
                        .Append(" SET ")
                        .Append(string.Join(",", columns.Select(kv =>
                        {
                            var storeItem = typeStore.PropertyStores.First(x => x.Name == kv.Key);

                            var value = storeItem.Member.GetValue(entry, null);

                            context.MemberName = storeItem.Member.Name;

                            var attrs = storeItem.Attributes.OfType<ValidationAttribute>();

                            if (attrs.Any())
                            {
                                ValidateValue(value, context, attrs);
                            }

                            var name = typeRegions.Tokens.ContainsKey(kv.Key)
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

            public IUpdateable<TEntity> From(Func<ITableInfo, string> table)
            {
                if (table is null)
                {
                    throw new ArgumentNullException(nameof(table));
                }

                from = (regions, source) => table.Invoke(regions);

                return this;
            }

            public IUpdateable<TEntity> From(Func<ITableInfo, TEntity, string> table)
            {
                from = table ?? throw new ArgumentNullException(nameof(table));

                return this;
            }

            public IUpdateable<TEntity> Limit(string[] columns)
            {
                limits = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IUpdateable<TEntity> Limit<TColumn>(Expression<Func<TEntity, TColumn>> columns) => Limit(DbRouter.Limit(columns));

            public IUpdateable<TEntity> Where(string[] columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                where = UpdateRowSource => columns;

                return this;
            }

            public IUpdateable<TEntity> Where<TColumn>(Expression<Func<TEntity, TColumn>> columns)
            {
                where = DbRouter.Where(columns);

                return this;
            }

            public override List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand()
            {
                var sb = new StringBuilder();
                var paramters = new Dictionary<string, object>();

                IEnumerable<KeyValuePair<string, string>> columns = defalutUpdateFields;

                if (!(limits is null))
                {
                    columns = columns.Where(x => limits.Any(y => string.Equals(y, x.Key, StringComparison.OrdinalIgnoreCase) || string.Equals(y, x.Value, StringComparison.OrdinalIgnoreCase)));
                }

                if (!(excepts is null))
                {
                    columns = columns.Where(x => !excepts.Any(y => string.Equals(y, x.Key, StringComparison.OrdinalIgnoreCase) || string.Equals(y, x.Value, StringComparison.OrdinalIgnoreCase)));
                }

                if (!columns.Any())
                    throw new DException("未指定更新字段!");

                int parameterCount = 0;

                var dicRoot = new List<KeyValuePair<TEntity, string[]>>();

                Entries.ForEach(item =>
                {
                    var wheres = where?.Invoke(item) ?? defaultWhere;

                    if (wheres.Length == 0)
                        throw new DException("未指定更新条件");

                    var whereColumns = typeRegions.ReadOrWrites
                            .Where(x => typeRegions.Keys.Contains(x.Key) || wheres.Any(y => string.Equals(y, x.Key, StringComparison.OrdinalIgnoreCase) || string.Equals(y, x.Value, StringComparison.OrdinalIgnoreCase)))
                            .Select(x => x.Key)
                            .ToArray();

                    if (whereColumns.Length == 0)
                        throw new DException("未指定更新条件!");

                    if (typeRegions.Tokens.Count > 0)
                    {
                        whereColumns = whereColumns
                        .Concat(typeRegions.ReadWrites
                            .Where(x => typeRegions.Tokens.ContainsKey(x.Key))
                            .Select(x => x.Key))
                        .Distinct()
                        .ToArray();
                    }

                    parameterCount += whereColumns.Length;

                    dicRoot.Add(new KeyValuePair<TEntity, string[]>(item, whereColumns));
                });

                var updateColumns = columns
                    .Union(typeRegions.ReadWrites
                        .Where(x => typeRegions.Tokens.ContainsKey(x.Key))
                        , Singleton<KeyValueStringComparer>.Instance)
                    .ToList();

                var results = new List<Tuple<string, Dictionary<string, ParameterValue>>>();

                parameterCount += updateColumns.Count * Entries.Count;

                if (parameterCount <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                {
                    results.Add(SqlGenerator(dicRoot, updateColumns));

                    return results;
                }

                parameterCount = 0;

                var dic = new List<KeyValuePair<TEntity, string[]>>();

                foreach (var kv in dicRoot.OrderBy(x => x.Value.Length))
                {
                    parameterCount += updateColumns.Count + kv.Value.Length;

                    if (parameterCount > MAX_PARAMETERS_COUNT)
                    {
                        parameterCount = updateColumns.Count + kv.Value.Length;

                        results.Add(SqlGenerator(dic, updateColumns));

                        dic.Clear();
                    }

                    dic.Add(new KeyValuePair<TEntity, string[]>(kv.Key, kv.Value));
                }

                if (dic.Count > 0)
                {
                    results.Add(SqlGenerator(dic, updateColumns));
                }

                return results;
            }
        }

        /// <summary>
        /// 赋予插入能力。
        /// </summary>
        /// <param name="executeable">分析器。</param>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public static IInsertable<TEntity> AsInsertable(IDatabase executeable, ICollection<TEntity> entries)
            => new Insertable(executeable, entries);

        /// <summary>
        /// 赋予更新能力。
        /// </summary>
        /// <param name="executeable">分析器。</param>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public static IUpdateable<TEntity> AsUpdateable(IDatabase executeable, ICollection<TEntity> entries)
            => new Updateable(executeable, entries);

        /// <summary>
        /// 赋予删除能力。
        /// </summary>
        /// <param name="executeable">分析器。</param>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public static IDeleteable<TEntity> AsDeleteable(IDatabase executeable, ICollection<TEntity> entries)
            => new Deleteable(executeable, entries);
    }
}
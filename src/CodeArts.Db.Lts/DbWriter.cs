using CodeArts.Db.Exceptions;
using CodeArts.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据写入器。
    /// </summary>
    public class DbWriter
    {
        // look for ? / @ / :
        private static readonly Regex smellsLikeOleDb = new Regex(@"(?<![\p{L}\p{N}@_])[?:@]([\p{L}\p{N}_][\p{L}\p{N}@_]*)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Dictionary<Type, DbType> typeMap;

        static DbWriter()
        {
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

        private static DbType LookupDbType(Type dataType)
        {
            if (dataType.IsEnum)
            {
                dataType = Enum.GetUnderlyingType(dataType);
            }
            else if (dataType.IsNullable())
            {
                dataType = Nullable.GetUnderlyingType(dataType);
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

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="param">参数。</param>
        public static void AddParameterAuto(IDbCommand command, object param = null)
        {
            switch (param)
            {
                case IDictionary<string, ParameterValue> parameters:
                    foreach (var kv in parameters)
                    {
                        AddParameterAuto(command, kv.Key, kv.Value);
                    }
                    break;
                case IDictionary<string, object> dic:
                    foreach (var kv in dic)
                    {
                        if (kv.Value is ParameterValue parameterValue)
                        {
                            AddParameterAuto(command, kv.Key, parameterValue);
                        }
                        else
                        {
                            AddParameterAuto(command, kv.Key, kv.Value);
                        }
                    }
                    break;
                default:
                    var matches = smellsLikeOleDb.Matches(command.CommandText);

                    if (matches.Count == 0)
                    {
                        return;
                    }

                    if (param is null)
                    {
                        throw new NotSupportedException();
                    }

                    var tokens = new List<string>(matches.Count);

                    foreach (Match item in matches)
                    {
                        tokens.Add(item.Groups[1].Value);
                    }

                    var paramType = param.GetType();

                    if (paramType.IsValueType || paramType == typeof(string))
                    {
                        if (matches.Count == 1)
                        {
                            AddParameterAuto(command, tokens[0], param);

                            break;
                        }

                        throw new NotSupportedException();
                    }

                    var storeItem = TypeItem.Get(paramType);

                    storeItem.PropertyStores
                        .Where(x => x.CanRead && tokens.Contains(x.Name))
                        .ForEach(x =>
                        {
                            var value = x.Member.GetValue(param, null);

                            if (value is null)
                            {
                                AddParameterAuto(command, x.Name, new ParameterValue(x.MemberType));
                            }
                            else if (value is ParameterValue parameterValue)
                            {
                                AddParameterAuto(command, x.Name, parameterValue);
                            }
                            else
                            {
                                AddParameterAuto(command, x.Name, value);
                            }
                        });
                    break;
            }
        }

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="key">参数名称。</param>
        /// <param name="value">参数值。</param>
        public static void AddParameterAuto(IDbCommand command, string key, ParameterValue value)
        {
            var dbParameter = command.CreateParameter();

            dbParameter.Value = value.IsNull ? DBNull.Value : value.Value;
            dbParameter.ParameterName = key;
            dbParameter.Direction = ParameterDirection.Input;
            dbParameter.DbType = LookupDbType(value.ValueType);

            command.Parameters.Add(dbParameter);
        }

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="key">参数名称。</param>
        /// <param name="value">参数值。</param>
        public static void AddParameterAuto(IDbCommand command, string key, object value)
        {
            var dbParameter = command.CreateParameter();

            switch (value)
            {
                case IDbDataParameter dbDataParameter when dbParameter is IDbDataParameter parameter:

                    parameter.Value = dbDataParameter.Value;
                    parameter.ParameterName = key;
                    parameter.Direction = dbDataParameter.Direction;
                    parameter.DbType = dbDataParameter.DbType;
                    parameter.SourceColumn = dbDataParameter.SourceColumn;
                    parameter.SourceVersion = dbDataParameter.SourceVersion;

                    if (dbParameter is System.Data.Common.DbParameter myParameter)
                    {
                        myParameter.IsNullable = dbDataParameter.IsNullable;
                    }

                    parameter.Scale = dbDataParameter.Scale;
                    parameter.Size = dbDataParameter.Size;
                    parameter.Precision = dbDataParameter.Precision;

                    command.Parameters.Add(dbParameter);
                    break;
                case IDataParameter dataParameter:
                    dbParameter.Value = dataParameter.Value;
                    dbParameter.ParameterName = key;
                    dbParameter.Direction = dataParameter.Direction;
                    dbParameter.DbType = dataParameter.DbType;
                    dbParameter.SourceColumn = dataParameter.SourceColumn;
                    dbParameter.SourceVersion = dataParameter.SourceVersion;
                    break;
                case ParameterValue parameterValue:
                    dbParameter.Value = parameterValue.IsNull ? DBNull.Value : parameterValue.Value;
                    dbParameter.ParameterName = key;
                    dbParameter.Direction = ParameterDirection.Input;
                    dbParameter.DbType = LookupDbType(parameterValue.ValueType);
                    break;
                default:
                    dbParameter.Value = value ?? DBNull.Value;
                    dbParameter.ParameterName = key;
                    dbParameter.Direction = ParameterDirection.Input;
                    dbParameter.DbType = value is null ? DbType.Object : LookupDbType(value.GetType());
                    break;
            }

            command.Parameters.Add(dbParameter);
        }
    }

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
            public DbRouteExecuter(IDbContext<TEntity> context, ICollection<TEntity> entries)
            {
                this.Entries = entries ?? throw new ArgumentNullException(nameof(entries));
                this.context = context ?? throw new ArgumentNullException(nameof(context));
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
            private readonly IDbContext<TEntity> context;
            private TransactionScopeOption? transactionScopeOption = null;
            protected static readonly ConcurrentDictionary<Type, object> DefaultCache = new ConcurrentDictionary<Type, object>();

            public IDbRouteExecuter<TEntity> UseTransaction(TransactionScopeOption transactionScopeOption)
            {
                this.transactionScopeOption = transactionScopeOption;

                return this;
            }

            public IDbRouter<TEntity> DbRouter => DbRouter<TEntity>.Instance;

            public ICollection<TEntity> Entries { get; }

            public ISQLCorrectSettings Settings => context.Settings;

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

                if (transactionScopeOption.HasValue || results.Count > 1)
                {
                    return UseTransactionExecute(results, commandTimeout);
                }

                return Execute(results, commandTimeout);
            }

            private int Execute(List<Tuple<string, Dictionary<string, ParameterValue>>> results, int? commandTimeout)
            {
                int influenceLine = 0;

                if (!commandTimeout.HasValue)
                {
                    using (var connection = context.CreateDb())
                    {
                        connection.Open();

                        results.ForEach(x =>
                        {
                            var commandSql = new CommandSql(x.Item1, x.Item2, commandTimeout);

                            SqlCapture.Current?.Capture(commandSql);

                            using (var command = connection.CreateCommand())
                            {
                                command.AllowSkippingFormattingSql = true;

                                command.CommandText = commandSql.Sql;

                                foreach (var kv in x.Item2)
                                {
                                    DbWriter.AddParameterAuto(command, kv.Key, kv.Value);
                                }

                                influenceLine += command.ExecuteNonQuery();
                            }
                        });
                    }

                    return influenceLine;
                }

                Stopwatch stopwatch = new Stopwatch();

                int remainingTime = commandTimeout.Value;

                using (var connection = context.CreateDb())
                {
                    connection.Open();

                    results.ForEach(x =>
                    {
                        var commandSql = new CommandSql(x.Item1, x.Item2, commandTimeout);

                        SqlCapture.Current?.Capture(commandSql);

                        using (var command = connection.CreateCommand())
                        {
                            command.AllowSkippingFormattingSql = true;

                            command.CommandText = commandSql.Sql;

                            command.CommandTimeout = remainingTime;

                            foreach (var kv in x.Item2)
                            {
                                DbWriter.AddParameterAuto(command, kv.Key, kv.Value);
                            }

                            stopwatch.Restart();

                            influenceLine += command.ExecuteNonQuery();

                            stopwatch.Stop();

                            remainingTime -= (int)(stopwatch.ElapsedMilliseconds / 1000);
                        }
                    });
                }

                return influenceLine;
            }

            private int UseTransactionExecute(List<Tuple<string, Dictionary<string, ParameterValue>>> results, int? commandTimeout)
            {
                using (TransactionScope transaction = new TransactionScope(transactionScopeOption ?? TransactionScopeOption.Required))
                {
                    int influenceLine = Execute(results, commandTimeout);

                    transaction.Complete();

                    return influenceLine;
                }
            }

            /// <summary>
            /// 准备命令。
            /// </summary>
            /// <returns></returns>
            public abstract List<Tuple<string, Dictionary<string, ParameterValue>>> PrepareCommand();

#if NET_NORMAL || NET_CORE
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

                if (transactionScopeOption.HasValue || results.Count > 1)
                {
                    return UseTransactionAsync(results, commandTimeout, cancellationToken);
                }

                return ExecutedAsync(results, commandTimeout, cancellationToken);
            }

            private async Task<int> ExecutedAsync(List<Tuple<string, Dictionary<string, ParameterValue>>> results, int? commandTimeout, CancellationToken cancellationToken = default)
            {
                int influenceLine = 0;

                if (!commandTimeout.HasValue)
                {
                    using (var connection = context.CreateDb())
                    {
                        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                        foreach (var x in results)
                        {
                            var commandSql = new CommandSql(x.Item1, x.Item2, commandTimeout);

                            using (var command = connection.CreateCommand())
                            {
                                command.AllowSkippingFormattingSql = true;

                                command.CommandText = commandSql.Sql;

                                foreach (var kv in x.Item2)
                                {
                                    DbWriter.AddParameterAuto(command, kv.Key, kv.Value);
                                }

                                influenceLine += await command.ExecuteNonQueryAsyc(cancellationToken).ConfigureAwait(false);
                            }
                        }
                    }

                    return influenceLine;
                }

                Stopwatch stopwatch = new Stopwatch();

                int remainingTime = commandTimeout.Value;

                using (var connection = context.CreateDb())
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var x in results)
                    {
                        var commandSql = new CommandSql(x.Item1, x.Item2, commandTimeout);

                        using (var command = connection.CreateCommand())
                        {
                            command.AllowSkippingFormattingSql = true;

                            command.CommandText = commandSql.Sql;

                            command.CommandTimeout = remainingTime;

                            foreach (var kv in x.Item2)
                            {
                                DbWriter.AddParameterAuto(command, kv.Key, kv.Value);
                            }

                            stopwatch.Restart();

                            influenceLine += await command.ExecuteNonQueryAsyc(cancellationToken).ConfigureAwait(false);

                            stopwatch.Stop();

                            remainingTime -= (int)(stopwatch.ElapsedMilliseconds / 1000);
                        }
                    }

                    return influenceLine;
                }
            }

            private async Task<int> UseTransactionAsync(List<Tuple<string, Dictionary<string, ParameterValue>>> results, int? commandTimeout, CancellationToken cancellationToken = default)
            {
                using (TransactionScope transaction = new TransactionScope(transactionScopeOption ?? TransactionScopeOption.Required))
                {
                    int influenceLine = await ExecutedAsync(results, commandTimeout, cancellationToken).ConfigureAwait(false);

                    transaction.Complete();

                    return influenceLine;
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
            public Deleteable(IDbContext<TEntity> context, ICollection<TEntity> entries) : base(context, entries)
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
                    if (kv.Key == value || kv.Value == value)
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
                        .Where(x => wheres.Any(y => y == x.Key) || wheres.Any(y => y == x.Value))
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

                           var parameterKey = index == 0 && groupIndex == 0
                           ? Settings.ParamterName(kv.Key.ToUrlCase())
                           : Settings.ParamterName($"{kv.Key.ToUrlCase()}_{groupIndex}_{index}");

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
                        .Where(x => wheres.Any(y => y == x.Key || y == x.Value))
                        .Select(x => x.Value)
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
                            return Complex(TableInfo.ReadWrites.Where(x => item.Key.Contains(x.Value)).ToList(), item, index, parameters);
                        }

                        string key = item.Key[0];

                        return Simple(TableInfo.ReadWrites.First(x => x.Value == key), item, index, parameters);
                    }));

                    results.Add(new Tuple<string, Dictionary<string, ParameterValue>>(sql, parameters));

                    return results;
                }

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

                        groupIndex = 0;

                        parameters.Clear();

                        parameterCount = item.Key.Length;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(';');
                    }

                    if (item.Key.Length > 1)
                    {
                        sb.Append(Complex(TableInfo.ReadWrites.Where(x => item.Key.Contains(x.Key)).ToList(), item, groupIndex, parameters));
                    }
                    else
                    {
                        string key = item.Key[0];

                        sb.Append(Simple(TableInfo.ReadWrites.First(x => x.Key == key), item, groupIndex, parameters));
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
            public Insertable(IDbContext<TEntity> context, ICollection<TEntity> entries) : base(context, entries)
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

                                    storeItem.Member.SetValue(item, value, null);
                                }
                            }

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
                    columns = columns.Where(x => limits.Any(y => y == x.Key || y == x.Value));
                }

                if (!(excepts is null))
                {
                    columns = columns.Where(x => !excepts.Any(y => y == x.Key || y == x.Value));
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
            public Updateable(IDbContext<TEntity> context, ICollection<TEntity> entries) : base(context, entries)
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
                    .Where(x => wheres.Any(y => y == x.Key || y == x.Value))
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
                            var name = typeRegions.Tokens.ContainsKey(kv.Key)
                            ? $"__token_{kv.Value}"
                            : kv.Value;

                            string parameterKey = index == 0
                            ? name
                            :
                            $"{name}_{index}";

                            var storeItem = typeStore.PropertyStores.First(x => x.Name == kv.Key);

                            var value = storeItem.Member.GetValue(entry, null);

                            context.MemberName = storeItem.Member.Name;

                            var attrs = storeItem.Attributes.OfType<ValidationAttribute>();

                            if (attrs.Any())
                            {
                                ValidateValue(value, context, attrs);
                            }

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
                    columns = columns.Where(x => limits.Any(y => y == x.Key || y == x.Value));
                }

                if (!(excepts is null))
                {
                    columns = columns.Where(x => !excepts.Any(y => y == x.Key || y == x.Value));
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
                            .Where(x => wheres.Any(y => y == x.Key || y == x.Value))
                            .Select(x => x.Value)
                            .ToArray();

                    if (whereColumns.Length == 0)
                        throw new DException("未指定更新条件!");

                    if (typeRegions.Tokens.Count > 0)
                    {
                        whereColumns = whereColumns
                        .Concat(typeRegions.ReadOrWrites
                            .Where(x => typeRegions.Tokens.ContainsKey(x.Key))
                            .Select(x => x.Value))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
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
        public static IInsertable<TEntity> AsInsertable(IDbContext<TEntity> executeable, ICollection<TEntity> entries)
            => new Insertable(executeable, entries);

        /// <summary>
        /// 赋予更新能力。
        /// </summary>
        /// <param name="executeable">分析器。</param>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public static IUpdateable<TEntity> AsUpdateable(IDbContext<TEntity> executeable, ICollection<TEntity> entries)
            => new Updateable(executeable, entries);

        /// <summary>
        /// 赋予删除能力。
        /// </summary>
        /// <param name="executeable">分析器。</param>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public static IDeleteable<TEntity> AsDeleteable(IDbContext<TEntity> executeable, ICollection<TEntity> entries)
            => new Deleteable(executeable, entries);
    }
}
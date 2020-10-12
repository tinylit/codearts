using CodeArts.ORM.Exceptions;
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
using System.Transactions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据写入器。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DbWriter<T> where T : class, IEntiy
    {
        /// <summary>
        /// 实体表信息
        /// </summary>
        public static ITableInfo TableInfo { get; }

        static DbWriter() => TableInfo = TableRegions.Resolve<T>();

        /// <summary>
        /// 最大参数长度
        /// </summary>
        public const int MAX_PARAMETERS_COUNT = 1000;
        /// <summary>
        /// 最大IN SQL 参数长度（取自 Oracle 9I）
        /// </summary>
        public const int MAX_IN_SQL_PARAMETERS_COUNT = 256;

        /// <summary>
        /// 数据验证
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="validationContext">验证上下文</param>
        /// <param name="validationAttributes">验证属性集合</param>
        private static void ValidateValue(object value, ValidationContext validationContext, IEnumerable<ValidationAttribute> validationAttributes)
        {
            //? 内容为空的时候不进行【数据类型和正则表达式验证】，内容空验证请使用【RequiredAttribute】。
            if (value is string text && string.IsNullOrEmpty(text))
            {
                validationAttributes = validationAttributes
                    .Except(validationAttributes.OfType<DataTypeAttribute>())
                    .Except(validationAttributes.OfType<RegularExpressionAttribute>());
            }

            DbValidator.ValidateValue(value, validationContext, validationAttributes);
        }

        /// <summary>
        /// 执行器
        /// </summary>
        private class Executeable : IExecuteable<T>, IExecuteProvider<T>
        {
            public Executeable(IWriteRepository<T> executer) => Executer = executer ?? throw new ArgumentNullException(nameof(executer));

            protected Executeable(IWriteRepository<T> executer, Expression expression) : this(executer) => _Expression = expression ?? throw new ArgumentNullException(nameof(expression));

            private readonly Expression _Expression = null;

            private Expression _ContextExpression = null;

            public IExecuteProvider<T> Provider => this;

            public IWriteRepository<T> Executer { get; }

            public Expression Expression => _Expression ?? _ContextExpression ?? (_ContextExpression = Expression.Constant(this));

            public IExecuteable<T> CreateExecute(Expression expression) => new Executeable(Executer, expression);

            public int Execute(Expression expression) => Executer.Excute(expression);
        }

        /// <summary>
        /// 路由执行力
        /// </summary>
        private abstract class RouteExecuteable : IRouteExecuteable<T>
        {
            public RouteExecuteable(IRouteExecuter executeable, IRouteProvider<T> provider, ICollection<T> entries)
            {
                this.Entries = entries ?? throw new ArgumentNullException(nameof(entries));
                this.Provider = provider ?? throw new ArgumentNullException(nameof(provider));
                this.executeable = executeable ?? throw new ArgumentNullException(nameof(executeable));
            }

            static RouteExecuteable()
            {
                typeStore = RuntimeTypeCache.Instance.GetCache<T>();
                typeRegions = TableRegions.Resolve<T>();
                defaultLimit = typeRegions.ReadWrites.Keys.ToArray();
                defaultWhere = typeRegions.Keys.ToArray();
            }

            protected static readonly TypeStoreItem typeStore;
            protected static readonly ITableInfo typeRegions;
            protected static readonly string[] defaultLimit;
            protected static readonly string[] defaultWhere;
            protected static readonly ConcurrentDictionary<Type, object> DefaultCache = new ConcurrentDictionary<Type, object>();
            private readonly IRouteExecuter executeable;

            public IRouteProvider<T> Provider { get; }

            public ICollection<T> Entries { get; }

            public ISQLCorrectSimSettings Settings => executeable.Settings;

            public int ExecuteCommand(int? commandTimeout = null)
            {
                if (Entries.Count > 0)
                {
                    return Execute(commandTimeout);
                }

                return 0;
            }

            public abstract int Execute(int? commandTimeout);

            public int Execute(string sql, Dictionary<string, object> param, int? commandTimeout) => executeable.ExecuteCommand(sql, param, commandTimeout);
        }

        private class Deleteable : RouteExecuteable, IDeleteable<T>
        {
            public Deleteable(IRouteExecuter<T> executeable, ICollection<T> entries) : base(executeable, executeable.CreateRouteProvider(CommandBehavior.Delete), entries)
            {
                wheres = defaultWhere;
            }

            private string[] wheres;
            private Func<T, string[]> where;
            private Func<ITableInfo, string> from;
            private TransactionScopeOption? option = TransactionScopeOption.Required;
            public override int Execute(int? commandTimeout)
            {
                if (where is null)
                {
                    return wheres.Length == 1 ? Simple(commandTimeout) : Complex(commandTimeout);
                }

                var dicRoot = new List<KeyValuePair<T, string>>();

                int parameter_count = 0;

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

                    parameter_count += columns.Length;

                    return string.Join(",", columns);

                }).ToList();

                var parameters = new Dictionary<string, object>();

                if (parameter_count <= MAX_PARAMETERS_COUNT)
                {
                    string sql = string.Join(";", listRoot.Select((item, index) =>
                    {
                        if (item.Key.IndexOf(',') > -1)
                        {
                            var keys = item.Key.Split(',');

                            return Complex(TableInfo.ReadWrites.Where(x => keys.Contains(x.Value)).ToList(), item, index, parameters);
                        }

                        return Simple(TableInfo.ReadWrites.First(x => x.Value == item.Key), item, index, parameters);
                    }));

                    return Transaction(() => Execute(sql, parameters, commandTimeout));
                }

                var sqls = new List<KeyValuePair<string, Dictionary<string, object>>>();

                int group_index = 0;

                parameter_count = 0;

                StringBuilder sb = new StringBuilder();

                foreach (var item in listRoot)
                {
                    if (parameter_count >= MAX_PARAMETERS_COUNT)
                    {
                        sqls.Add(new KeyValuePair<string, Dictionary<string, object>>(sb.ToString(), parameters));

                        group_index = 0;

                        parameter_count = 0;

                        sb = new StringBuilder();

                        parameters = new Dictionary<string, object>();
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(';');
                    }

                    if (item.Key.IndexOf(',') > -1)
                    {
                        string[] keys = item.Key.Split(',');

                        parameter_count += keys.Length;

                        if (parameter_count > MAX_PARAMETERS_COUNT)
                        {
                            sqls.Add(new KeyValuePair<string, Dictionary<string, object>>(sb.ToString(), parameters));

                            group_index = 0;

                            parameter_count = keys.Length;

                            sb = new StringBuilder();

                            parameters = new Dictionary<string, object>();
                        }

                        sb.Append(Complex(TableInfo.ReadWrites.Where(x => keys.Contains(x.Key)).ToList(), item, group_index, parameters));
                    }
                    else
                    {
                        parameter_count++;

                        sb.Append(Simple(TableInfo.ReadWrites.First(x => x.Key == item.Key), item, group_index, parameters));
                    }

                    group_index++;
                }

                if (sb.Length > 0)
                {
                    sqls.Add(new KeyValuePair<string, Dictionary<string, object>>(sb.ToString(), parameters));
                }

                return Transaction(() =>
                 {
                     int affected_rows = 0;

                     if (commandTimeout.HasValue)
                     {
                         var sw = new Stopwatch();

                         var milliseconds = commandTimeout.Value * 1000L;

                         foreach (var kv in sqls)
                         {
                             sw.Start();

                             affected_rows += Execute(kv.Key, kv.Value, commandTimeout);

                             sw.Stop();

                             if (sw.ElapsedMilliseconds > milliseconds)
                             {
                                 throw new TimeoutException();
                             }
                         }

                         return affected_rows;
                     }

                     foreach (var kv in sqls)
                     {
                         affected_rows += Execute(kv.Key, kv.Value, commandTimeout);
                     }

                     return affected_rows;
                 });
            }

            private int Transaction(Func<int> factroy)
            {
                if (!option.HasValue)
                {
                    return factroy.Invoke();
                }

                using (TransactionScope transaction = new TransactionScope(option.Value))
                {
                    int affected_rows = factroy.Invoke();

                    transaction.Complete();

                    return affected_rows;
                }
            }

            private int Simple(int? commandTimeout)
            {
                bool flag = true;
                string value = wheres[0];
                KeyValuePair<string, string> column = default;

                foreach (var kv in typeRegions.ReadWrites)
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

                if (Entries.Count <= MAX_PARAMETERS_COUNT)
                {
                    var parameters = new Dictionary<string, object>();

                    var sql = Simple(column, Entries, 0, parameters);

                    return Transaction(() => Execute(sql, parameters, commandTimeout));
                }

                var sqls = new List<KeyValuePair<string, Dictionary<string, object>>>();

                for (int i = 0; i < Entries.Count; i += MAX_PARAMETERS_COUNT)
                {
                    var parameters = new Dictionary<string, object>();

                    var sql = Simple(column, Entries.Skip(i).Take(MAX_PARAMETERS_COUNT), 0, parameters);

                    sqls.Add(new KeyValuePair<string, Dictionary<string, object>>(sql, parameters));
                }

                return Transaction(() =>
                {
                    int affected_rows = 0;

                    if (commandTimeout.HasValue)
                    {
                        var sw = new Stopwatch();

                        var milliseconds = commandTimeout.Value * 1000L;

                        foreach (var kv in sqls)
                        {
                            sw.Start();

                            affected_rows += Execute(kv.Key, kv.Value, commandTimeout);

                            sw.Stop();

                            if (sw.ElapsedMilliseconds > milliseconds)
                            {
                                throw new TimeoutException();
                            }
                        }
                    }

                    foreach (var kv in sqls)
                    {
                        affected_rows += Execute(kv.Key, kv.Value, commandTimeout);
                    }

                    return affected_rows;
                });
            }

            private int Complex(int? commandTimeout)
            {
                var columns = typeRegions.ReadWrites
                        .Where(x => wheres.Any(y => y == x.Key) || wheres.Any(y => y == x.Value))
                        .ToList();

                if (Entries.Count * columns.Count <= MAX_PARAMETERS_COUNT)
                {
                    var parameters = new Dictionary<string, object>();

                    var sql = Complex(columns, Entries, 0, parameters);

                    return Transaction(() => Execute(sql, parameters, commandTimeout));
                }

                int offset = MAX_PARAMETERS_COUNT / columns.Count;

                var sqls = new List<KeyValuePair<string, Dictionary<string, object>>>();

                for (int i = 0; i < Entries.Count; i += offset)
                {
                    var parameters = new Dictionary<string, object>();

                    var sql = Complex(columns, Entries.Skip(i).Take(offset), 0, parameters);

                    sqls.Add(new KeyValuePair<string, Dictionary<string, object>>(sql, parameters));
                }

                return Transaction(() =>
                {
                    int affected_rows = 0;

                    if (commandTimeout.HasValue)
                    {
                        var sw = new Stopwatch();

                        var milliseconds = commandTimeout.Value * 1000L;

                        foreach (var kv in sqls)
                        {
                            sw.Start();

                            affected_rows += Execute(kv.Key, kv.Value, commandTimeout);

                            sw.Stop();

                            if (sw.ElapsedMilliseconds > milliseconds)
                            {
                                throw new TimeoutException();
                            }
                        }
                    }

                    foreach (var kv in sqls)
                    {
                        affected_rows += Execute(kv.Key, kv.Value, commandTimeout);
                    }

                    return affected_rows;
                });
            }

            private string Simple(KeyValuePair<string, string> column, IEnumerable<T> collect, int group, Dictionary<string, object> parameters)
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

                     var parameterKey = group == 0 && index == 0 ?
                         name
                         :
                         $"{name}_{group}_{index}";

                     parameters.Add(parameterKey, value);

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

            private string Complex(List<KeyValuePair<string, string>> columns, IEnumerable<T> collect, int group, Dictionary<string, object> parameters)
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

                           var parameterKey = group == 0 && index == 0 ?
                           Settings.ParamterName(kv.Key.ToUrlCase())
                           :
                           Settings.ParamterName($"{kv.Key.ToUrlCase()}_{group}_{index}");

                           parameters.Add(parameterKey, value);

                           return string.Concat(Settings.Name(kv.Value), "=", parameterKey);
                       })), ")");
                   })));

                return sb.ToString();
            }

            public IDeleteable<T> From(Func<ITableInfo, string> table)
            {
                from = table;

                return this;
            }

            public IDeleteable<T> Where(string[] columns)
            {
                wheres = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IDeleteable<T> Where<TColumn>(Expression<Func<T, TColumn>> columns)
            {
                where = Provider.Where(columns);

                return this;
            }

            public IDeleteable<T> UseTransaction(TransactionScopeOption option)
            {
                this.option = option;

                return this;
            }
        }

        private class Insertable : RouteExecuteable, IInsertable<T>
        {
            public Insertable(IRouteExecuter<T> executeable, ICollection<T> entries) : base(executeable, executeable.CreateRouteProvider(CommandBehavior.Insert), entries)
            {
            }

            private string[] limits;
            private string[] excepts;
            private Func<ITableInfo, string> from;
            private TransactionScopeOption? option = TransactionScopeOption.Required;

            public IInsertable<T> Except(string[] columns)
            {
                excepts = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IInsertable<T> Except<TColumn>(Expression<Func<T, TColumn>> columns) => Except(Provider.Except(columns));

            public override int Execute(int? commandTimeout)
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

                var insert_columns = columns.ToList();

                int parameter_count = Entries.Count * insert_columns.Count;

                if (parameter_count <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                {
                    var kv = SqlGenerator(Entries, insert_columns);

                    return Transaction(() => Execute(kv.Key, kv.Value, commandTimeout));
                }

                int offset = MAX_PARAMETERS_COUNT / insert_columns.Count;
                var sqls = new List<KeyValuePair<string, Dictionary<string, object>>>();

                for (int i = 0; i < Entries.Count; i += offset)
                {
                    sqls.Add(SqlGenerator(Entries.Skip(i).Take(offset).ToList(), insert_columns));
                }

                return Transaction(() =>
                {
                    int affected_rows = 0;

                    if (commandTimeout.HasValue)
                    {
                        var sw = new Stopwatch();

                        var milliseconds = commandTimeout.Value * 1000L;

                        foreach (var kv in sqls)
                        {
                            sw.Start();

                            affected_rows += Execute(kv.Key, kv.Value, commandTimeout);

                            sw.Stop();

                            if (sw.ElapsedMilliseconds > milliseconds)
                            {
                                throw new TimeoutException();
                            }
                        }
                    }

                    foreach (var kv in sqls)
                    {
                        affected_rows += Execute(kv.Key, kv.Value, commandTimeout);
                    }

                    return affected_rows;
                });
            }

            private KeyValuePair<string, Dictionary<string, object>> SqlGenerator(IEnumerable<T> list, List<KeyValuePair<string, string>> columns)
            {
                var sb = new StringBuilder();
                var paramters = new Dictionary<string, object>();

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

                                value = storeItem.Member.GetValue(item, null);
                            }

                            paramters.Add(parameterKey, value);

                            return Settings.ParamterName(parameterKey);
                        })), ")");
                    })));

                return new KeyValuePair<string, Dictionary<string, object>>(sb.ToString(), paramters);
            }

            public IInsertable<T> From(Func<ITableInfo, string> table)
            {
                from = table ?? throw new ArgumentNullException(nameof(table));

                return this;
            }

            public IInsertable<T> Limit(string[] columns)
            {
                limits = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IInsertable<T> Limit<TColumn>(Expression<Func<T, TColumn>> columns) => Limit(Provider.Limit(columns));

            private int Transaction(Func<int> factroy)
            {
                if (!option.HasValue)
                {
                    return factroy.Invoke();
                }

                using (TransactionScope transaction = new TransactionScope(option.Value))
                {
                    int affected_rows = factroy.Invoke();

                    transaction.Complete();

                    return affected_rows;
                }
            }

            public IInsertable<T> UseTransaction(TransactionScopeOption option)
            {
                this.option = option;

                return this;
            }
        }

        private class Updateable : RouteExecuteable, IUpdateable<T>
        {
            public Updateable(IRouteExecuter<T> executeable, ICollection<T> entries) : base(executeable, executeable.CreateRouteProvider(CommandBehavior.Update), entries)
            {
            }

            private string[] limits;
            private string[] excepts;
            private Func<T, string[]> where;
            private Func<ITableInfo, T, string> from;
            private TransactionScopeOption? option = TransactionScopeOption.Required;

            public IUpdateable<T> Except(string[] columns)
            {
                excepts = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IUpdateable<T> Except<TColumn>(Expression<Func<T, TColumn>> columns) => Except(Provider.Except(columns));

            public override int Execute(int? commandTimeout)
            {
                var sb = new StringBuilder();
                var paramters = new Dictionary<string, object>();

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
                    throw new DException("未指定更新字段!");

                columns = columns.Union(typeRegions.ReadWrites
                        .Where(x => typeRegions.Tokens.ContainsKey(x.Key))
                    );

                int parameter_count = 0;

                var dicRoot = new List<KeyValuePair<T, string[]>>();

                Entries.ForEach(item =>
                {
                    var wheres = where?.Invoke(item) ?? defaultWhere;

                    if (wheres.Length == 0)
                        throw new DException("未指定更新条件");

                    var where_columns = typeRegions.ReadWrites
                            .Where(x => wheres.Any(y => y == x.Key || y == x.Value))
                            .Select(x => x.Value)
                            .ToArray();

                    if (where_columns.Length == 0)
                        throw new DException("未指定更新条件!");

                    if (typeRegions.Tokens.Count > 0)
                    {
                        where_columns = where_columns
                        .Concat(typeRegions.ReadOrWrites
                            .Where(x => typeRegions.Tokens.ContainsKey(x.Key))
                            .Select(x => x.Value))
                        .Distinct()
                        .ToArray();
                    }

                    parameter_count += where_columns.Length;

                    dicRoot.Add(new KeyValuePair<T, string[]>(item, where_columns));
                });

                var update_columns = columns
                    .Union(typeRegions.ReadWrites
                        .Where(x => typeRegions.Tokens.ContainsKey(x.Key))
                    )
                    .ToList();

                parameter_count += update_columns.Count * Entries.Count;

                if (parameter_count <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                {
                    var kv = SqlGenerator(dicRoot, update_columns);

                    return Transaction(() => Execute(kv.Key, kv.Value, commandTimeout));
                }

                parameter_count = 0;

                var sqls = new List<KeyValuePair<string, Dictionary<string, object>>>();

                var dic = new List<KeyValuePair<T, string[]>>();

                foreach (var item in dicRoot)
                {
                    parameter_count += update_columns.Count + item.Value.Length;

                    if (parameter_count > MAX_PARAMETERS_COUNT)
                    {
                        sqls.Add(SqlGenerator(dic, update_columns));

                        dic = new List<KeyValuePair<T, string[]>>();

                        parameter_count = update_columns.Count + item.Value.Length;
                    }

                    dic.Add(new KeyValuePair<T, string[]>(item.Key, item.Value));
                }

                if (dic.Count > 0)
                {
                    sqls.Add(SqlGenerator(dic, update_columns));
                }

                return Transaction(() =>
                 {
                     int affected_rows = 0;

                     if (commandTimeout.HasValue)
                     {
                         var sw = new Stopwatch();

                         var milliseconds = commandTimeout.Value * 1000L;

                         foreach (var kv in sqls)
                         {
                             sw.Start();

                             affected_rows += Execute(kv.Key, kv.Value, commandTimeout);

                             sw.Stop();

                             if (sw.ElapsedMilliseconds > milliseconds)
                             {
                                 throw new TimeoutException();
                             }
                         }

                         return affected_rows;
                     }

                     foreach (var kv in sqls)
                     {
                         affected_rows += Execute(kv.Key, kv.Value, commandTimeout);
                     }

                     return affected_rows;
                 });
            }

            private KeyValuePair<string, Dictionary<string, object>> SqlGenerator(List<KeyValuePair<T, string[]>> list, List<KeyValuePair<string, string>> columns)
            {
                var sb = new StringBuilder();
                var paramters = new Dictionary<string, object>();

                list.ForEach((kvr, index) =>
                {
                    var entry = kvr.Key;

                    var wheres = kvr.Value;

                    var context = new ValidationContext(entry, null, null);

                    string whereStr = string.Join(" AND ", typeRegions.ReadWrites
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

                            value = storeItem.Member.GetValue(entry, null);
                        }

                        paramters.Add(parameterKey, value);

                        if (typeRegions.Tokens.TryGetValue(kv.Key, out TokenAttribute token))
                        {
                            value = token.Create();

                            if (value is null)
                                throw new NoNullAllowedException("令牌不允许为空!");

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

                                value = storeItem.Member.GetValue(entry, null);
                            }

                            paramters.Add(parameterKey, value);

                            return string.Concat(Settings.Name(kv.Value), "=", Settings.ParamterName(parameterKey));

                        })))
                        .Append(" WHERE ")
                        .Append(whereStr)
                        .Append(";");
                });

                return new KeyValuePair<string, Dictionary<string, object>>(sb.ToString(), paramters);
            }

            public IUpdateable<T> From(Func<ITableInfo, string> table)
            {
                if (table is null)
                {
                    throw new ArgumentNullException(nameof(table));
                }

                from = (regions, source) => table.Invoke(regions);

                return this;
            }

            public IUpdateable<T> From(Func<ITableInfo, T, string> table)
            {
                from = table ?? throw new ArgumentNullException(nameof(table));

                return this;
            }

            public IUpdateable<T> Limit(string[] columns)
            {
                limits = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IUpdateable<T> Limit<TColumn>(Expression<Func<T, TColumn>> columns) => Limit(Provider.Limit(columns));

            public IUpdateable<T> Where(string[] columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                where = UpdateRowSource => columns;

                return this;
            }

            public IUpdateable<T> Where<TColumn>(Expression<Func<T, TColumn>> columns)
            {
                where = Provider.Where(columns);

                return this;
            }

            private int Transaction(Func<int> factroy)
            {
                if (!option.HasValue)
                {
                    return factroy.Invoke();
                }

                using (TransactionScope transaction = new TransactionScope(option.Value))
                {
                    int affected_rows = factroy.Invoke();

                    transaction.Complete();

                    return affected_rows;
                }
            }

            public IUpdateable<T> UseTransaction(TransactionScopeOption option)
            {
                this.option = option;

                return this;
            }
        }

        /// <summary>
        /// 赋予插入能力。
        /// </summary>
        /// <param name="executeable">分析器。</param>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public static IInsertable<T> AsInsertable(IRouteExecuter<T> executeable, ICollection<T> entries)
            => new Insertable(executeable, entries);

        /// <summary>
        /// 赋予更新能力。
        /// </summary>
        /// <param name="executeable">分析器。</param>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public static IUpdateable<T> AsUpdateable(IRouteExecuter<T> executeable, ICollection<T> entries)
            => new Updateable(executeable, entries);

        /// <summary>
        /// 赋予删除能力。
        /// </summary>
        /// <param name="executeable">分析器。</param>
        /// <param name="entries">集合。</param>
        /// <returns></returns>
        public static IDeleteable<T> AsDeleteable(IRouteExecuter<T> executeable, ICollection<T> entries)
            => new Deleteable(executeable, entries);

        /// <summary>
        /// 赋予执行能力。
        /// </summary>
        /// <param name="editable">编辑。</param>
        /// <returns></returns>
        public static IExecuteable<T> AsExecuteable(IWriteRepository<T> editable)
            => new Executeable(editable);
    }
}

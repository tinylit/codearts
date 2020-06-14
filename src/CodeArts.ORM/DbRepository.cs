using CodeArts.ORM.Exceptions;
using CodeArts.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Transactions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据仓储（支持增删查改）
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public class DbRepository<T> : Repository<T>, IDbRepository<T>, IRepository<T>, IOrderedQueryable<T>, IQueryable<T>, IEditable<T>, IEnumerable<T>, IOrderedQueryable, IQueryable, IQueryProvider, IEditable, IEnumerable where T : class, IEntiy
    {
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
        /// 数据库执行器
        /// </summary>
        protected virtual IDbRepositoryExecuter DbExecuter => DbConnectionManager.Create(DbAdapter);

        /// <summary>
        /// 执行器
        /// </summary>
        private class Executeable : IExecuteable<T>, IExecuteProvider<T>
        {
            public Executeable(IEditable<T> executer) => Executer = executer ?? throw new ArgumentNullException(nameof(executer));

            protected Executeable(IEditable<T> executer, Expression expression) : this(executer) => _Expression = expression ?? throw new ArgumentNullException(nameof(expression));

            private readonly Expression _Expression = null;

            private Expression _ContextExpression = null;

            public IExecuteProvider<T> Provider => this;

            public IEditable<T> Executer { get; }

            public Expression Expression => _Expression ?? _ContextExpression ?? (_ContextExpression = Expression.Constant(this));

            public IExecuteable<T> CreateExecute(Expression expression) => new Executeable(Executer, expression);

            public int Execute(Expression expression) => Executer.Excute(expression);
        }

        /// <summary>
        /// 路由执行力
        /// </summary>
        private abstract class RouteExecuteable : IRouteExecuteable<T>, IEnumerable<T>, IEnumerable
        {
            public RouteExecuteable(ISQLCorrectSimSettings settings, IRouteExecuteProvider<T> provider, IEnumerable<T> collect, Func<string, Dictionary<string, object>, int?, int> executer)
            {
                this.collect = collect ?? throw new ArgumentNullException(nameof(collect));
                this.executer = executer ?? throw new ArgumentNullException(nameof(executer));
                this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
                Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            }

            static RouteExecuteable()
            {
                typeStore = RuntimeTypeCache.Instance.GetCache<T>();
                typeRegions = MapperRegions.Resolve<T>();
                defaultLimit = typeRegions.ReadWrites.Keys.ToArray();
                defaultWhere = typeRegions.Keys.ToArray();
            }

            protected static readonly TypeStoreItem typeStore;
            protected static readonly ITableInfo typeRegions;
            protected static readonly string[] defaultLimit;
            protected static readonly string[] defaultWhere;
            protected static readonly ConcurrentDictionary<Type, object> DefaultCache = new ConcurrentDictionary<Type, object>();
            private readonly IEnumerable<T> collect;
            private readonly Func<string, Dictionary<string, object>, int?, int> executer;

            public IRouteExecuteProvider<T> Provider { get; }

            public ISQLCorrectSimSettings Settings { get; private set; }

            public int ExecuteCommand(int? commandTimeout = null)
            {
                if (collect.Any())
                {
                    return Execute(commandTimeout);
                }

                return 0;
            }

            public abstract int Execute(int? commandTimeout);

            public int Execute(string sql, Dictionary<string, object> param, int? commandTimeout) => executer.Invoke(sql, param, commandTimeout);

            public IEnumerator<T> GetEnumerator() => collect.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class Deleteable : RouteExecuteable, IDeleteable<T>
        {
            public Deleteable(ISQLCorrectSimSettings settings, IRouteExecuteProvider<T> provider, IEnumerable<T> collect, Func<string, Dictionary<string, object>, int?, int> executer) : base(settings, provider, collect, executer)
            {
                wheres = defaultWhere;
            }

            private string[] wheres;
            private Func<T, string[]> where;
            private Func<ITableInfo, string> from;

            public override int Execute(int? commandTimeout)
            {
                if (where is null)
                {
                    return wheres.Length == 1 ? Simple(commandTimeout) : Complex(commandTimeout);
                }

                var list = this.ToList();

                var dicRoot = new List<KeyValuePair<T, string>>();

                int parameter_count = 0;

                var listRoot = list.GroupBy(item =>
                {
                    var wheres = where.Invoke(item);

                    if (wheres.Length == 0)
                        throw new DException("未指定删除条件!");

                    var columns = typeRegions.ReadOrWrites
                        .Where(x => wheres.Any(y => y == x.Key || y == x.Value))
                        .Select(x => x.Value)
                        .ToArray();

                    if (columns.Length == 0)
                        throw new DException("未指定删除条件!");

                    parameter_count += columns.Length;

                    return string.Join(",", columns);

                }).ToList();

                if (parameter_count <= MAX_PARAMETERS_COUNT)
                {
                    var parameters = new Dictionary<string, object>();

                    string sql = string.Join(";", listRoot.Select((item, index) =>
                    {
                        if (item.Key.IndexOf(',') > -1)
                        {
                            var keys = item.Key.Split(',');

                            return Complex(TableInfo.ReadWrites.Where(x => keys.Contains(x.Value)).ToList(), item, index, parameters);
                        }

                        return Simple(TableInfo.ReadWrites.First(x => x.Value == item.Key), item, index, parameters);
                    }));

                    return Execute(sql, parameters, commandTimeout);
                }

                int group_index = 0;
                int affected_rows = 0;

                parameter_count = 0;
                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    StringBuilder sb = new StringBuilder();
                    var parameters = new Dictionary<string, object>();

                    foreach (var item in listRoot)
                    {
                        if (parameter_count >= MAX_PARAMETERS_COUNT)
                        {
                            affected_rows += Execute(sb.ToString(), parameters, commandTimeout);

                            sb.Clear();

                            group_index = 0;

                            parameter_count = 0;

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
                                affected_rows += Execute(sb.ToString(), parameters, commandTimeout);

                                sb.Clear();

                                group_index = 0;

                                parameter_count = keys.Length;

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

                    affected_rows += Execute(sb.ToString(), parameters, commandTimeout);

                    transaction.Complete();
                }

                return affected_rows;
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
                    throw new DException("未指定删除条件!");

                var list = this.ToList();

                if (list.Count <= MAX_PARAMETERS_COUNT)
                {
                    var parameters = new Dictionary<string, object>();

                    var sql = Simple(column, list, 0, parameters);

                    return Execute(sql, parameters, commandTimeout);
                }

                int affected_rows = 0;

                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    for (int i = 0; i < list.Count; i += MAX_PARAMETERS_COUNT)
                    {
                        var parameters = new Dictionary<string, object>();

                        var sql = Simple(column, list.Skip(i).Take(MAX_PARAMETERS_COUNT), 0, parameters);

                        affected_rows += Execute(sql, parameters, commandTimeout);
                    }

                    transaction.Complete();
                }

                return affected_rows;
            }

            private int Complex(int? commandTimeout)
            {
                var columns = typeRegions.ReadWrites
                        .Where(x => wheres.Any(y => y == x.Key) || wheres.Any(y => y == x.Value))
                        .ToList();

                var list = this.ToList();

                if (list.Count * columns.Count <= MAX_PARAMETERS_COUNT)
                {
                    var parameters = new Dictionary<string, object>();

                    var sql = Complex(columns, list, 0, parameters);

                    return Execute(sql, parameters, commandTimeout);
                }

                int affected_rows = 0;
                int offset = MAX_PARAMETERS_COUNT / columns.Count;

                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    for (int i = 0; i < list.Count; i += offset)
                    {
                        var parameters = new Dictionary<string, object>();

                        var sql = Complex(columns, list.Skip(i).Take(offset), 0, parameters);

                        affected_rows += Execute(sql, parameters, commandTimeout);
                    }

                    transaction.Complete();
                }

                return affected_rows;
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

                     ValidateValue(value, context, storeItem.Attributes.OfType<ValidationAttribute>());

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
                        .Append(list.First())
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

                           ValidateValue(value, context, storeItem.Attributes.OfType<ValidationAttribute>());

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
        }

        private class Insertable : RouteExecuteable, IInsertable<T>
        {
            public Insertable(ISQLCorrectSimSettings settings, IRouteExecuteProvider<T> provider, IEnumerable<T> collect, Func<string, Dictionary<string, object>, int?, int> executer) : base(settings, provider, collect, executer)
            {
            }

            private string[] limits;
            private string[] excepts;
            private Func<ITableInfo, string> from;

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
                    throw new DException("未指定插入字段!");

                var list = this.ToList();

                var insert_columns = columns.ToList();

                int parameter_count = list.Count * insert_columns.Count;

                if (parameter_count <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                {
                    return Execute(list, insert_columns, commandTimeout);
                }

                int affected_rows = 0;
                int offset = MAX_PARAMETERS_COUNT / insert_columns.Count;

                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    for (int i = 0; i < list.Count; i += offset)
                    {
                        affected_rows += Execute(list.Skip(i).Take(offset).ToList(), insert_columns, commandTimeout);
                    }

                    transaction.Complete();
                }

                return affected_rows;
            }

            private int Execute(List<T> list, List<KeyValuePair<string, string>> columns, int? commandTimeout)
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
                                        throw new NoNullAllowedException("令牌不允许为空!");

                                    storeItem.Member.SetValue(item, value, null);
                                }
                            }

                            context.MemberName = storeItem.Member.Name;

                            ValidateValue(value, context, storeItem.Attributes.OfType<ValidationAttribute>());

                            paramters.Add(parameterKey, value);

                            return Settings.ParamterName(parameterKey);
                        })), ")");
                    })));

                return Execute(sb.Append(";").ToString(), paramters, commandTimeout);
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
        }

        private class Updateable : RouteExecuteable, IUpdateable<T>
        {
            public Updateable(ISQLCorrectSimSettings settings, IRouteExecuteProvider<T> provider, IEnumerable<T> collect, Func<string, Dictionary<string, object>, int?, int> executer) : base(settings, provider, collect, executer)
            {
            }

            private string[] limits;
            private string[] excepts;
            private Func<T, string[]> where;
            private Func<ITableInfo, T, string> from;

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

                var list = this.ToList();

                var dicRoot = new List<KeyValuePair<T, string[]>>();

                list.ForEach(item =>
                {
                    var wheres = where.Invoke(item) ?? defaultWhere;

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

                parameter_count += update_columns.Count * list.Count;

                if (parameter_count <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                {
                    return Execute(dicRoot, update_columns, commandTimeout);
                }

                int affected_rows = 0;

                parameter_count = 0;

                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    var dic = new List<KeyValuePair<T, string[]>>();

                    foreach (var item in dicRoot)
                    {
                        parameter_count += update_columns.Count + item.Value.Length;

                        if (parameter_count > MAX_PARAMETERS_COUNT)
                        {
                            affected_rows += Execute(dic, update_columns, commandTimeout);

                            dic = new List<KeyValuePair<T, string[]>>();

                            parameter_count = update_columns.Count + item.Value.Length;
                        }

                        dic.Add(new KeyValuePair<T, string[]>(item.Key, item.Value));
                    }

                    affected_rows += Execute(dic, update_columns, commandTimeout);

                    transaction.Complete();
                }

                return affected_rows;
            }

            private int Execute(List<KeyValuePair<T, string[]>> list, List<KeyValuePair<string, string>> columns, int? commandTimeout)
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

                        ValidateValue(value, context, storeItem.Attributes.OfType<ValidationAttribute>());

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

                            ValidateValue(value, context, storeItem.Attributes.OfType<ValidationAttribute>());

                            paramters.Add(parameterKey, value);

                            return string.Concat(Settings.Name(kv.Value), "=", Settings.ParamterName(parameterKey));

                        })))
                        .Append(" WHERE ")
                        .Append(whereStr)
                        .Append(";");
                });

                return Execute(sb.ToString(), paramters, commandTimeout);
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
        }

        /// <summary>
        /// 插入、更新、删除执行器
        /// </summary>
        /// <returns></returns>
        public IExecuteable<T> AsExecuteable() => new Executeable(this);

        /// <summary>
        /// 创建路由执行器
        /// </summary>
        /// <returns></returns>
        protected virtual IRouteExecuteProvider<T> CreateRouteExecuteProvider() => RouteExecuter<T>.Instance;

        /// <summary>
        /// 插入路由执行器
        /// </summary>
        /// <param name="item">项目</param>
        /// <returns></returns>
        public IInsertable<T> AsInsertable(T item) => AsInsertable(new T[1] { item ?? throw new ArgumentNullException(nameof(item)) });

        /// <summary>
        /// 插入路由执行器
        /// </summary>
        /// <param name="collect">项目集合</param>
        /// <returns></returns>
        public virtual IInsertable<T> AsInsertable(IEnumerable<T> collect) => new Insertable(Settings, CreateRouteExecuteProvider(), collect ?? throw new ArgumentNullException(nameof(collect)), Execute);

        /// <summary>
        /// 更新路由执行器
        /// </summary>
        /// <param name="item">项目</param>
        /// <returns></returns>
        public IUpdateable<T> AsUpdateable(T item) => AsUpdateable(new T[1] { item ?? throw new ArgumentNullException(nameof(item)) });

        /// <summary>
        /// 更新路由执行器
        /// </summary>
        /// <param name="collect">项目集合</param>
        /// <returns></returns>
        public virtual IUpdateable<T> AsUpdateable(IEnumerable<T> collect) => new Updateable(Settings, CreateRouteExecuteProvider(), collect ?? throw new ArgumentNullException(nameof(collect)), Execute);

        /// <summary>
        /// 删除路由执行器
        /// </summary>
        /// <param name="item">项目</param>
        /// <returns></returns>
        public IDeleteable<T> AsDeleteable(T item) => AsDeleteable(new T[1] { item ?? throw new ArgumentNullException(nameof(item)) });

        /// <summary>
        /// 删除路由执行器
        /// </summary>
        /// <param name="collect">项目集合</param>
        /// <returns></returns>
        public virtual IDeleteable<T> AsDeleteable(IEnumerable<T> collect) => new Deleteable(Settings, CreateRouteExecuteProvider(), collect ?? throw new ArgumentNullException(nameof(collect)), Execute);

        /// <summary>
        /// 表达式分析
        /// </summary>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        int IEditable<T>.Excute(Expression expression) => DbExecuter.Execute<T>(Connection, expression);

        /// <summary>
        /// 执行语句
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        public int Insert(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (ExecuteAuthorize(sql, CommandTypes.Insert))
            {
                return Execute(sql, param, commandTimeout);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// 执行语句
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        public int Update(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (ExecuteAuthorize(sql, CommandTypes.Update))
            {
                return Execute(sql, param, commandTimeout);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// 执行语句
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns></returns>
        public int Delete(SQL sql, object param = null, int? commandTimeout = null)
        {
            if (ExecuteAuthorize(sql, CommandTypes.Delete))
            {
                return Execute(sql, param, commandTimeout);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// 执行SQL验证。
        /// </summary>
        /// <returns></returns>
        protected virtual bool ExecuteAuthorize(ISQL sql, UppercaseString commandType)
        {
            if (sql.Tables.All(x => x.CommandType == CommandTypes.Select))
            {
                return false;
            }

            return sql.Tables.All(x => x.CommandType == CommandTypes.Select || x.CommandType == commandType && string.Equals(x.Name, TableInfo.TableName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 执行SQL验证
        /// </summary>
        /// <returns></returns>
        protected virtual bool ExecuteAuthorize(ISQL sql)
        {
            if (sql.Tables.All(x => x.CommandType == CommandTypes.Select))
            {
                return false;
            }

            return sql.Tables.All(x => x.CommandType == CommandTypes.Select || string.Equals(x.Name, TableInfo.TableName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 执行。
        /// </summary>
        /// <param name="sql">SQL。</param>
        /// <param name="param">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        private int Execute(string sql, Dictionary<string, object> param, int? commandTimeout = null)
        {
            return DbExecuter.Execute(Connection, sql, param, commandTimeout);
        }

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <param name="commandTimeout">超时时间</param>
        /// <returns>影响行</returns>
        protected virtual int Execute(ISQL sql, object param = null, int? commandTimeout = null)
        {
            if (!ExecuteAuthorize(sql))
            {
                throw new NonAuthorizedException();
            }

            if (param is null)
            {
                if (sql.Parameters.Count > 0)
                    throw new DSyntaxErrorException("参数不匹配!");

                return Execute(sql.ToString(Settings), null, commandTimeout);
            }

            var type = param.GetType();

            if (type.IsValueType || type == typeof(string))
            {
                if (sql.Parameters.Count > 1)
                    throw new DSyntaxErrorException("参数不匹配!");

                var token = sql.Parameters.First();

                return Execute(sql.ToString(Settings), new Dictionary<string, object>
                {
                    [token.Name] = param
                }, commandTimeout);
            }

            if (!(param is Dictionary<string, object> parameters))
            {
                parameters = param.MapTo<Dictionary<string, object>>();
            }

            if (!sql.Parameters.All(x => parameters.Any(y => y.Key == x.Name)))
                throw new DSyntaxErrorException("参数不匹配!");

            return Execute(sql.ToString(Settings), parameters, commandTimeout);
        }
    }
}

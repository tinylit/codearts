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
    public class DbRepository<T> : Repository<T>, IRepository<T>, IOrderedQueryable<T>, IQueryable<T>, IEditable<T>, IEnumerable<T>, IOrderedQueryable, IQueryable, IQueryProvider, IEditable, IEnumerable where T : class, IEntiy
    {
        private IDbRepositoryExecuter _DbExecuter = null;

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
        protected virtual IDbRepositoryExecuter DbExecuter => _DbExecuter ?? (_DbExecuter = DbConnectionManager.Create(DbAdapter));

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
            public RouteExecuteable(IEditable executer, IRouteExecuteProvider provider, IEnumerable<T> collect)
            {
                Editable = executer ?? throw new ArgumentNullException(nameof(executer));
                this.collect = collect ?? throw new ArgumentNullException(nameof(collect));
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
            protected static readonly ITableRegions typeRegions;
            protected static readonly string[] defaultLimit;
            protected static readonly string[] defaultWhere;
            protected static readonly ConcurrentDictionary<Type, object> DefaultCache = new ConcurrentDictionary<Type, object>();

            private readonly IEnumerable<T> collect;

            public IRouteExecuteProvider Provider { get; }

            public IEditable Editable { get; }

            protected ISQLCorrectSimSettings Settings => Editable.Settings;

            public int ExecuteCommand()
            {
                if (!collect.Any())
                    return 0;

                return Execute();
            }

            public abstract int Execute();

            public IEnumerator<T> GetEnumerator() => collect.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class Deleteable : RouteExecuteable, IDeleteable<T>
        {
            public Deleteable(IEditable executer, IRouteExecuteProvider provider, IEnumerable<T> collect) : base(executer, provider, collect)
            {
                wheres = defaultWhere;
            }

            private string[] wheres;
            private Func<T, string[]> where;
            private Func<ITableRegions, string> from;

            public override int Execute()
            {
                if (where is null)
                {
                    return wheres.Length == 1 ? Simple() : Complex();
                }

                var list = this.ToList();

                var dicRoot = new List<KeyValuePair<T, string>>();

                int parameter_count = 0;

                var listRoot = list.GroupBy(item =>
                {
                    var wheres = where.Invoke(item);

                    if (wheres.Length == 0)
                        throw new DException("未指定删除条件!");

                    var columns = typeRegions.ReadWrites
                        .Where(x => wheres.Any(y => y == x.Key) || wheres.Any(y => y == x.Value))
                        .OrderBy(x => x.Key)
                        .Select(x => x.Key)
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

                            return Complex(TableRegions.ReadWrites.Where(x => keys.Contains(x.Key)).ToList(), item, index, parameters);
                        }

                        return Simple(TableRegions.ReadWrites.First(x => x.Key == item.Key), item, index, parameters);
                    }));

                    return Editable.Excute(sql, parameters);
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
                            affected_rows += Editable.Excute(sb.ToString(), parameters);

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
                                affected_rows += Editable.Excute(sb.ToString(), parameters);

                                sb.Clear();

                                group_index = 0;

                                parameter_count = keys.Length;

                                parameters = new Dictionary<string, object>();
                            }

                            sb.Append(Complex(TableRegions.ReadWrites.Where(x => keys.Contains(x.Key)).ToList(), item, group_index, parameters));
                        }
                        else
                        {
                            parameter_count++;

                            sb.Append(Simple(TableRegions.ReadWrites.First(x => x.Key == item.Key), item, group_index, parameters));
                        }

                        group_index++;
                    }

                    affected_rows += Editable.Excute(sb.ToString(), parameters);

                    transaction.Complete();
                }

                return affected_rows;
            }

            private int Simple()
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

                    return Editable.Excute(sql, parameters);
                }

                int affected_rows = 0;

                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    for (int i = 0; i < list.Count; i += MAX_PARAMETERS_COUNT)
                    {
                        var parameters = new Dictionary<string, object>();

                        var sql = Simple(column, list.Skip(i).Take(MAX_PARAMETERS_COUNT), 0, parameters);

                        affected_rows += Editable.Excute(sql, parameters);
                    }

                    transaction.Complete();
                }

                return affected_rows;
            }

            private int Complex()
            {
                var columns = typeRegions.ReadWrites
                        .Where(x => wheres.Any(y => y == x.Key) || wheres.Any(y => y == x.Value))
                        .ToList();

                var list = this.ToList();

                if (list.Count * columns.Count <= MAX_PARAMETERS_COUNT)
                {
                    var parameters = new Dictionary<string, object>();

                    var sql = Complex(columns, list, 0, parameters);

                    return Editable.Excute(sql, parameters);
                }

                int affected_rows = 0;
                int offset = MAX_PARAMETERS_COUNT / columns.Count;

                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    for (int i = 0; i < list.Count; i += offset)
                    {
                        var parameters = new Dictionary<string, object>();

                        var sql = Complex(columns, list.Skip(i).Take(offset), 0, parameters);

                        affected_rows += Editable.Excute(sql, parameters);
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

            public IDeleteable<T> From(Func<ITableRegions, string> table)
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
            public Insertable(IEditable executer, IRouteExecuteProvider provider, IEnumerable<T> collect) : base(executer, provider, collect)
            {
            }

            private string[] limits;
            private string[] excepts;
            private Func<ITableRegions, string> from;

            public IInsertable<T> Except(string[] columns)
            {
                excepts = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IInsertable<T> Except<TColumn>(Expression<Func<T, TColumn>> columns) => Except(Provider.Except(columns));

            public override int Execute()
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
                    return Execute(list, insert_columns);
                }

                int affected_rows = 0;
                int offset = MAX_PARAMETERS_COUNT / insert_columns.Count;

                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    for (int i = 0; i < list.Count; i += offset)
                    {
                        affected_rows += Execute(list.Skip(i).Take(offset).ToList(), insert_columns);
                    }

                    transaction.Complete();
                }

                return affected_rows;
            }

            private int Execute(List<T> list, List<KeyValuePair<string, string>> columns)
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

                return Editable.Excute(sb.Append(";").ToString(), paramters);
            }

            public IInsertable<T> From(Func<ITableRegions, string> table)
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
            public Updateable(IEditable executer, IRouteExecuteProvider provider, IEnumerable<T> collect) : base(executer, provider, collect)
            {
            }

            private string[] limits;
            private string[] excepts;
            private Func<T, string[]> where;
            private Func<ITableRegions, T, string> from;

            public IUpdateable<T> Except(string[] columns)
            {
                excepts = columns ?? throw new ArgumentNullException(nameof(columns));

                return this;
            }

            public IUpdateable<T> Except<TColumn>(Expression<Func<T, TColumn>> columns) => Except(Provider.Except(columns));

            public override int Execute()
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

                columns = columns.Union(typeRegions.ReadWrites.Where(x => typeRegions.Tokens.Any(y => x.Key == y.Key)));

                int parameter_count = 0;

                var list = this.ToList();

                var dicRoot = new List<KeyValuePair<T, string[]>>();

                list.ForEach(item =>
                {
                    var wheres = where.Invoke(item) ?? defaultWhere;

                    if (wheres.Length == 0)
                        throw new DException("未指定更新条件");

                    var where_columns = typeRegions.ReadWrites
                            .Where(x => wheres.Any(y => y == x.Key) || wheres.Any(y => y == x.Value))
                            .OrderBy(x => x.Key)
                            .Select(x => x.Key)
                            .ToArray();

                    if (where_columns.Length == 0)
                        throw new DException("未指定删除条件!");

                    parameter_count += where_columns.Length;

                    dicRoot.Add(new KeyValuePair<T, string[]>(item, where_columns));
                });

                int token_count = typeRegions.Tokens.Count;

                var insert_columns = columns.Union(typeRegions.ReadWrites.Where(x => typeRegions.Tokens.Any(y => x.Key == y.Key))).ToList();

                parameter_count += (insert_columns.Count + token_count) * list.Count;

                if (parameter_count <= MAX_PARAMETERS_COUNT) // 所有数据库的参数个数最小限制 => 取自 Oracle 9i
                {
                    return Execute(dicRoot, insert_columns);
                }

                int affected_rows = 0;

                parameter_count = 0;

                using (TransactionScope transaction = new TransactionScope(TransactionScopeOption.RequiresNew))
                {
                    var dic = new List<KeyValuePair<T, string[]>>();

                    foreach (var item in dicRoot)
                    {
                        parameter_count += insert_columns.Count + token_count + item.Value.Length;

                        if (parameter_count > MAX_PARAMETERS_COUNT)
                        {
                            affected_rows += Execute(dic, insert_columns);

                            dic = new List<KeyValuePair<T, string[]>>();

                            parameter_count = insert_columns.Count + token_count + item.Value.Length;
                        }

                        dic.Add(new KeyValuePair<T, string[]>(item.Key, item.Value));
                    }

                    affected_rows += Execute(dic, insert_columns);

                    transaction.Complete();
                }

                return affected_rows;
            }

            private int Execute(List<KeyValuePair<T, string[]>> list, List<KeyValuePair<string, string>> columns)
            {
                var sb = new StringBuilder();
                var paramters = new Dictionary<string, object>();

                list.ForEach((kvr, index) =>
                {
                    var entry = kvr.Key;

                    var wheres = kvr.Value;

                    var context = new ValidationContext(entry, null, null);

                    string whereStr = string.Join(" AND ", typeRegions.ReadWrites
                    .Where(x => wheres.Any(y => y == x.Key) || wheres.Any(y => y == x.Value) || typeRegions.Tokens.Any(y => x.Key == y.Key))
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
                            var name = typeRegions.Tokens.ContainsKey(kv.Key) ? $"__token_{kv.Key.ToUrlCase()}" : kv.Key.ToUrlCase();

                            string parameterKey = index == 0 ?
                                name
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

                return Editable.Excute(sb.ToString(), paramters);
            }

            public IUpdateable<T> From(Func<ITableRegions, string> table)
            {
                if (table is null)
                {
                    throw new ArgumentNullException(nameof(table));
                }

                from = (regions, source) => table.Invoke(regions);

                return this;
            }

            public IUpdateable<T> From(Func<ITableRegions, T, string> table)
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
        /// 路由提供器
        /// </summary>
        private class RouteExecuteProvider : IRouteExecuteProvider
        {
            private static Func<TEntry, string[]> Conditional<TEntry>(ParameterExpression parameter, Expression test, MemberExpression ifTrue, MemberExpression ifFalse)
            {
                var bodyExp = Expression.Condition(test, Expression.Constant(ifTrue.Member.Name), Expression.Constant(ifFalse.Member.Name));

                var lamdaExp = Expression.Lambda<Func<TEntry, string>>(bodyExp, parameter);

                var invoke = lamdaExp.Compile();

                return source => new string[] { invoke.Invoke(source) };
            }

            public string[] Except<TEntry, TColumn>(Expression<Func<TEntry, TColumn>> lamda)
            {
                if (lamda.Parameters.Count > 1)
                    throw new ExpressionNotSupportedException();

                var parameter = lamda.Parameters.First();

                var body = lamda.Body;

                switch (body.NodeType)
                {
                    case ExpressionType.Constant when body is ConstantExpression constant:
                        switch (constant.Value)
                        {
                            case string text:
                                return text.Split(',', ' ');
                            case string[] arr:
                                return arr;
                            default:
                                throw new NotImplementedException();
                        }
                    case ExpressionType.MemberAccess when body is MemberExpression member:
                        return new string[] { member.Member.Name };
                    case ExpressionType.MemberInit when body is MemberInitExpression memberInit:
                        return memberInit.Bindings.Select(x => x.Member.Name).ToArray();
                    case ExpressionType.New when body is NewExpression newExpression:
                        return newExpression.Members.Select(x => x.Name).ToArray();
                    case ExpressionType.Parameter:
                        var storeItem = RuntimeTypeCache.Instance.GetCache(parameter.Type);
                        return storeItem.PropertyStores
                            .Where(x => x.CanRead && x.CanWrite)
                            .Select(x => x.Name)
                            .ToArray();
                }
                throw new NotImplementedException();
            }

            public string[] Limit<TEntry, TColumn>(Expression<Func<TEntry, TColumn>> lamda) => Except(lamda);

            public Func<TEntry, string[]> Where<TEntry, TColumn>(Expression<Func<TEntry, TColumn>> lamda)
            {
                if (lamda.Parameters.Count > 1)
                    throw new ExpressionNotSupportedException();

                var parameter = lamda.Parameters.First();

                var body = lamda.Body;

                switch (body.NodeType)
                {
                    case ExpressionType.Coalesce when body is BinaryExpression binary && binary.Left is MemberExpression left && binary.Right is MemberExpression right:
                        return Conditional<TEntry>(parameter, Expression.NotEqual(left, Expression.Default(binary.Type)), left, right);
                    case ExpressionType.Conditional when body is ConditionalExpression conditional && conditional.IfTrue is MemberExpression left && conditional.IfFalse is MemberExpression right:
                        return Conditional<TEntry>(parameter, conditional.Test, left, right);
                    case ExpressionType.NewArrayInit when body.Type == typeof(string[]):
                    case ExpressionType.Call when body is MethodCallExpression methodCall && methodCall.Method.ReturnType == typeof(string[]):
                        return lamda.Compile() as Func<TEntry, string[]> ?? throw new NotImplementedException();
                    default:

                        var columns = Except(lamda);

                        return source => columns;
                }
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
        protected virtual IRouteExecuteProvider CreateRouteExecuteProvider() => RuntimeServManager.Singleton<IRouteExecuteProvider, RouteExecuteProvider>();

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
        public virtual IInsertable<T> AsInsertable(IEnumerable<T> collect) => new Insertable(this, CreateRouteExecuteProvider(), collect ?? throw new ArgumentNullException(nameof(collect)));

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
        public virtual IUpdateable<T> AsUpdateable(IEnumerable<T> collect) => new Updateable(this, CreateRouteExecuteProvider(), collect ?? throw new ArgumentNullException(nameof(collect)));

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
        public virtual IDeleteable<T> AsDeleteable(IEnumerable<T> collect) => new Deleteable(this, CreateRouteExecuteProvider(), collect ?? throw new ArgumentNullException(nameof(collect)));

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
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        int IEditable.Excute(string sql, Dictionary<string, object> parameters) => DbExecuter.Execute(Connection, sql, parameters);

        /// <summary>
        /// 执行SQL验证
        /// </summary>
        /// <returns></returns>
        protected virtual bool ExcuteAuthorize(ISQL sql) => sql.Tables.All(x => x.CommandType == CommandTypes.Select || string.Equals(x.Name, TableRegions.TableName, StringComparison.OrdinalIgnoreCase)) && sql.Tables.Any(x => string.Equals(x.Name, TableRegions.TableName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="sql">SQL</param>
        /// <param name="param">参数</param>
        /// <returns>影响行</returns>
        protected virtual int Excute(ISQL sql, object param = null)
        {
            if (!ExcuteAuthorize(sql))
                throw new NonAuthorizeException();

            if (param is null)
            {
                if (sql.Parameters.Count > 0)
                    throw new DSyntaxErrorException("参数不匹配!");

                return DbExecuter.Execute(Connection, sql.ToString(Settings));
            }

            var type = param.GetType();

            if (type.IsValueType || type == typeof(string))
            {
                if (sql.Parameters.Count > 1)
                    throw new DSyntaxErrorException("参数不匹配!");

                var token = sql.Parameters.First();

                return DbExecuter.Execute(Connection, sql.ToString(Settings), new Dictionary<string, object>
                {
                    [token.Name] = param
                });
            }

            if (!(param is Dictionary<string, object> parameters))
            {
                parameters = param.MapTo<Dictionary<string, object>>();
            }

            if (!sql.Parameters.All(x => parameters.Any(y => y.Key == x.Name)))
                throw new DSyntaxErrorException("参数不匹配!");

            return DbExecuter.Execute(Connection, sql.ToString(Settings), parameters);
        }
    }
}

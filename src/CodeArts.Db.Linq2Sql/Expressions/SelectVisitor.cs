using CodeArts.Db.Exceptions;
using CodeArts.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
#if NET40
using System.Collections.ObjectModel;
#endif
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.Db.Expressions
{
    /// <summary>
    /// 查询。
    /// </summary>
    public class SelectVisitor : CoreVisitor
    {
        #region 匿名内部类。
        /// <summary>
        /// 智能开关。
        /// </summary>
        private class OrderBySwitch
        {
            private bool isFirst = true;

            private readonly Action firstAction;
            private readonly Action unFirstAction;

            public OrderBySwitch(Action firstAction, Action unFirstAction)
            {
                this.firstAction = firstAction;
                this.unFirstAction = unFirstAction;
            }

            public void OrderBy()
            {
                if (isFirst)
                {
                    isFirst = false;

                    firstAction.Invoke();
                }
                else
                {
                    unFirstAction.Invoke();
                }
            }
        }
        #endregion

        private int take = -1;

        private int skip = -1;

        /// <summary>
        /// 条件。
        /// </summary>
        private readonly OrderBySwitch orderBySwitch;

        /// <summary>
        /// <see cref="Queryable.Any{TSource}(IQueryable{TSource})"/>
        /// <seealso cref="Queryable.Any{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// <seealso cref="Queryable.All{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// </summary>
        private bool isExists = false;

        /// <summary>
        /// 是否构建 SELECT 部分。
        /// </summary>
        private bool buildSelect = true;

        /// <summary>
        /// 已经构建了Select
        /// </summary>
        private bool buildedSelect = false;

        /// <summary>
        /// 是否构建From。
        /// </summary>
        private bool buildFrom = true;

        /// <summary>
        /// 在查询部分。
        /// </summary>
        private bool inSelect = false;

        /// <summary>
        /// 构建表。
        /// </summary>
        private bool buildTable = true;

        /// <summary>
        /// 是否包装。
        /// </summary>
        private bool isNoPackage = true;

        /// <summary>
        /// 有组合函数。
        /// </summary>
        private bool hasCombination = false;

        /// <summary>
        /// 用链接函数。
        /// </summary>
        private bool hasJoin = false;

        /// <summary>
        /// 去重。
        /// </summary>
        private bool isDistinct = false;

        /// <summary>
        /// 逆序。
        /// </summary>
        private bool reverseOrder = false;

        /// <summary>
        /// 使用了OrderBy
        /// </summary>
        private bool useOrderBy = false;

        /// <summary>
        /// 使用了统计函数。
        /// </summary>
        private bool useCount = false;

        /// <summary>
        /// 使用了聚合函数。
        /// </summary>
        private bool useAggregation = false;

        /// <summary>
        /// 使用GroupBy。
        /// </summary>
        private GroupByVisitor byVisitor;

        /// <summary>
        /// 使用了GroupBy。
        /// </summary>
        private bool useGroupBy = false;

        /// <summary>
        /// 使用了GROUP BY 聚合函数。
        /// </summary>
        private bool useGroupByAggregation = false;

        /// <summary>
        /// 使用了类型转换。
        /// </summary>
        private bool useCast = false;

        /// <summary>
        /// 有效成员。
        /// </summary>
        private List<string> memberFilters = new List<string>();

        /// <summary>
        /// 类型关系。
        /// </summary>
        private readonly Dictionary<Type, Type> castCache = new Dictionary<Type, Type>();

        /// <summary>
        /// 分组字段集合。
        /// </summary>
        private readonly Dictionary<MemberInfo, Expression> groupByExpressions = new Dictionary<MemberInfo, Expression>();

        /// <summary>
        /// 链接表别名。
        /// </summary>
        private readonly Dictionary<Type, List<string>> joinCache = new Dictionary<Type, List<string>>();

        private sealed class TupleEqualityComparer : IEqualityComparer<Tuple<Type, string>>
        {
            private TupleEqualityComparer() { }

            public static TupleEqualityComparer Instance = new TupleEqualityComparer();
            public bool Equals(Tuple<Type, string> x, Tuple<Type, string> y)
            {
                if (x is null)
                {
                    return y is null;
                }

                if (y is null)
                {
                    return false;
                }

                return x.Item1 == y.Item1 && x.Item2 == y.Item2;
            }

            public int GetHashCode(Tuple<Type, string> obj) => obj?.Item1.GetHashCode() ?? 0;
        }

        /// <summary>
        /// 默认值。
        /// </summary>
        private readonly Dictionary<Tuple<Type, string>, Expression> defaultCache = new Dictionary<Tuple<Type, string>, Expression>(TupleEqualityComparer.Instance);

        /// <inheritdoc />
        public SelectVisitor(BaseVisitor visitor, bool isNewWriter = true) : base(visitor, isNewWriter)
        {
            orderBySwitch = new OrderBySwitch(writer.OrderBy, writer.Delimiter);
        }

        /// <inheritdoc />
        public SelectVisitor(ISQLCorrectSettings settings) : base(settings)
        {
            orderBySwitch = new OrderBySwitch(writer.OrderBy, writer.Delimiter);
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node)
        => node.Method.DeclaringType == Types.Queryable || node.Method.DeclaringType == Types.RepositoryExtentions;

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            base.StartupCore(node);

            if (buildSelect)
            {
                throw new DSyntaxErrorException();
            }
        }

        /// <inheritdoc />
        protected override void VisitCore(MethodCallExpression node)
        {
            string name = node.Method.Name;

            if (isNoPackage)
            {
                switch (name)
                {
                    case MethodCall.TimeOut:
                    case MethodCall.From:
                    case MethodCall.Any:
                    case MethodCall.All:
                    case MethodCall.Contains:
                    case MethodCall.Union:
                    case MethodCall.Concat:
                    case MethodCall.Except:
                    case MethodCall.Intersect:
                        break;
                    default:
                        isNoPackage = false;
                        break;
                }
            }


            switch (name)
            {
                case MethodCall.ElementAt:
                case MethodCall.ElementAtOrDefault:

                    base.Visit(node.Arguments[0]);

                    int index = (int)node.Arguments[1].GetValueFromExpression();

                    if (index < 0)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    if (this.take > 0 && index < this.take)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    this.take = 1;

                    this.skip += index;

                    break;
                case MethodCall.Take:
                case MethodCall.TakeLast:

                    if (useAggregation)
                    {
                        throw new DSyntaxErrorException($"使用聚合函数时，禁止使用分页函数({name})!");
                    }

                    if (name == MethodCall.TakeLast)
                    {
                        reverseOrder ^= true;
                    }

                    int take = (int)node.Arguments[1].GetValueFromExpression();

                    if (take < 1)
                    {
                        throw new ArgumentOutOfRangeException($"使用{name}函数,参数值必须大于零!");
                    }

                    if (this.take > 0 && take < this.take)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    if (this.skip > -1)
                    {
                        if (this.skip > take)
                        {
                            throw new IndexOutOfRangeException();
                        }

                        take -= this.skip;
                    }

                    if (this.take == -1)
                    {
                        this.take = take;
                    }

                    base.Visit(node.Arguments[0]);

                    if (!useOrderBy && name == MethodCall.TakeLast)
                    {
                        throw new DSyntaxErrorException($"使用函数({name})时，必须使用排序函数(OrderBy/OrderByDescending)!");
                    }

                    break;
                case MethodCall.First:
                case MethodCall.FirstOrDefault:
                case MethodCall.Single:
                case MethodCall.SingleOrDefault:

                    // TOP(1)
                    this.take = 1;

                    if (node.Arguments.Count > 1)
                    {
                        VisitCondition(node);
                    }
                    else
                    {
                        Visit(node.Arguments[0]);
                    }

                    break;
                case MethodCall.Last:
                case MethodCall.LastOrDefault:

                    // TOP(..)
                    this.take = 1;

                    reverseOrder ^= true;

                    if (node.Arguments.Count > 1)
                    {
                        VisitCondition(node);
                    }
                    else
                    {
                        base.Visit(node.Arguments[0]);
                    }

                    if (!useOrderBy)
                    {
                        throw new DSyntaxErrorException($"使用函数({name})时，必须使用排序函数(OrderBy/OrderByDescending)!");
                    }

                    break;
                case MethodCall.Max:
                    buildSelect = false;
                    buildedSelect = true;
                    useAggregation = true;
                    using (var visitor = new MaxVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Min:
                    buildSelect = false;
                    buildedSelect = true;
                    useAggregation = true;
                    using (var visitor = new MinVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Sum:
                    buildSelect = false;
                    buildedSelect = true;
                    useAggregation = true;
                    using (var visitor = new SumVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Average:
                    buildSelect = false;
                    buildedSelect = true;
                    useAggregation = true;
                    using (var visitor = new AverageVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Count:
                case MethodCall.LongCount:
                    useCount = true;
                    buildSelect = false;
                    buildedSelect = true;
                    useAggregation = true;
                    using (var visitor = new CountVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Skip:
                case MethodCall.SkipLast:

                    if (useAggregation)
                    {
                        throw new DSyntaxErrorException($"使用聚合函数时，禁止使用分页函数({name})!");
                    }

                    if (name == MethodCall.SkipLast)
                    {
                        reverseOrder ^= true;
                    }

                    int skip = (int)node.Arguments[1].GetValueFromExpression();

                    if (skip < 0)
                    {
                        throw new ArgumentOutOfRangeException($"使用({name})函数,参数值不能小于零!");
                    }

                    if (this.skip == -1)
                    {
                        this.skip = skip;
                    }
                    else
                    {
                        this.skip += skip;
                    }

                    base.Visit(node.Arguments[0]);

                    if (!useOrderBy && name == MethodCall.SkipLast)
                    {
                        throw new DSyntaxErrorException($"使用函数({name})时，必须使用排序函数(OrderBy/OrderByDescending)!");
                    }

                    break;
                case MethodCall.Reverse:

                    reverseOrder ^= true;

                    base.Visit(node.Arguments[0]);

                    if (!useOrderBy)
                    {
                        throw new DSyntaxErrorException($"使用函数“{name}”时，必须使用排序函数(OrderBy/OrderByDescending)!");
                    }

                    break;
                case MethodCall.GroupBy:
                case MethodCall.Where when node.Arguments[0].Type.IsGroupingQueryable():
                case MethodCall.TakeWhile when node.Arguments[0].Type.IsGroupingQueryable():
                case MethodCall.SkipWhile when node.Arguments[0].Type.IsGroupingQueryable():

                    if (name == MethodCall.GroupBy)
                    {
                        castCache.Add(node.Type.GetGenericArguments().First(), TypeToEntryType(node.Arguments[0].Type));
                    }

                    if (!useGroupBy)
                    {
                        byVisitor = new GroupByVisitor(this, defaultCache, groupByExpressions);
                    }

                    useGroupBy = true;

                    byVisitor.Startup(node);

                    break;
                case MethodCall.Select:

                    if (useCount)
                    {
                        base.Visit(node.Arguments[0]);

                        break;
                    }

                    if (!buildSelect)
                    {
                        throw new DSyntaxErrorException($"请将函数“{name}”置于查询最后一个包含入参的函数之后!");
                    }

                    buildSelect = buildFrom = false;

                    writer.Select();

                    Workflow(() =>
                    {
                        buildedSelect = true;

                        if (isDistinct)
                        {
                            writer.Distinct();
                        }

                        inSelect = true;

                        Visit(node.Arguments[1]);

                        inSelect = false;

                        writer.From();

                        if (!hasJoin && !hasCombination)
                        {
                            WriteTableName(node.Arguments[0].Type);
                        }

                    }, () => base.Visit(node.Arguments[0]));

                    break;
                case MethodCall.SelectMany when node.Arguments.Count == 3:

                    hasJoin = true;

                    var parameterExp = Join(node.Arguments[2], true);

                    bool DoneLeftJoin(ParameterExpression parameter, Expression expression)
                    {
                        if (expression.NodeType == ExpressionType.MemberAccess)
                        {
                            return false;
                        }

                        switch (expression)
                        {
                            case UnaryExpression unary:
                                return DoneLeftJoin(parameter, unary.Operand);
                            case LambdaExpression lambda when lambda.Parameters.Count == 1:
                                return DoneLeftJoin(parameter, lambda.Body);
                            case MethodCallExpression methodCall when methodCall.Method.Name == MethodCall.DefaultIfEmpty:

                                if (methodCall.Arguments.Count > 1)
                                {
                                    defaultCache.Add(Tuple.Create(parameter.Type, parameter.Name), methodCall.Arguments[1]);
                                }

                                return true;
                            default:
                                throw new DSyntaxErrorException();
                        }
                    }

                    using (var visitor = new GroupJoinVisitor(this, parameterExp, DoneLeftJoin(parameterExp, node.Arguments[1])))
                    {
                        visitor.Startup(node.Arguments[0]);
                    }

                    buildTable = false;

                    break;
                case MethodCall.Distinct:

                    if (buildedSelect)
                    {
                        throw new DSyntaxErrorException($"函数“{name}”未生效！");
                    }

                    isDistinct = true;

                    base.Visit(node.Arguments[0]);

                    break;
                case MethodCall.Cast:
                case MethodCall.OfType:
                    Type type = node.Type
                        .GetGenericArguments()
                        .First();

                    if (type.IsValueType || type == typeof(string) || typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        throw new TypeAccessInvalidException($"“{node.Method.Name}”函数泛型参数类型不能是值类型、字符串类型或迭代类型!");
                    }

                    var objExp = node.Arguments[0];

                    var originalType = objExp.Type;

                    if (node.Type == originalType)
                    {
                        base.Visit(objExp);

                        break;
                    }

                    useCast = true;

                    if (!castCache.ContainsKey(type))
                    {
                        castCache.Add(type, TypeToEntryType(originalType));
                    }

                    var entry = TypeItem.Get(type);

                    if (memberFilters.Count == 0)
                    {
                        memberFilters.AddRange(entry.PropertyStores
                            .Where(x => x.CanRead && x.CanWrite)
                            .Select(x => x.Name.ToLower()));
                    }
                    else //? 取交集
                    {
                        memberFilters = memberFilters
                           .Intersect(entry.PropertyStores
                           .Where(x => x.CanRead && x.CanWrite)
                           .Select(x => x.Name.ToLower()))
                           .ToList();
                    }

                    if (memberFilters.Count == 0)
                    {
                        throw new DException("未指定查询字段!");
                    }

                    base.Visit(objExp);

                    break;
                case MethodCall.OrderBy:
                case MethodCall.ThenBy:
                case MethodCall.OrderByDescending:
                case MethodCall.ThenByDescending:

                    useOrderBy = true;

                    if (useAggregation)
                    {
                        base.Visit(node.Arguments[0]);

                        break;
                    }

                    bool thatReverseOrder = reverseOrder;

                    Workflow(() => writer.UsingSort(() =>
                    {
                        orderBySwitch.OrderBy();

                        base.Visit(node.Arguments[1]);

                        if (thatReverseOrder ^ node.Method.Name.EndsWith("Descending"))
                        {
                            writer.Descending();
                        }

                    }), () => base.Visit(node.Arguments[0]));

                    break;
                case MethodCall.Join:

                    hasJoin = true;

                    Join(node.Arguments[2], false);

                    Join(node.Arguments[3], true);

                    using (var visitor = new JoinVisitor(this))
                    {
                        visitor.Startup(node);
                    }

                    buildTable = false;
                    break;
                case MethodCall.Any:
                    isExists = true;
                    using (var visitor = new AnyVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.All:
                    isExists = true;
                    using (var visitor = new AllVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Union:
                case MethodCall.Concat:
                case MethodCall.Except:
                case MethodCall.Intersect:

                    buildTable = buildFrom = false;
                    hasCombination = true;

                    if (isNoPackage)
                    {
                        buildSelect = false;
                        buildedSelect = true;
                        using (var visitor = new CombinationVisitor(this))
                        {
                            visitor.Startup(node);
                        }

                        break;
                    }

                    string prefix = "x";

                    if (buildSelect)
                    {
                        buildSelect = false;

                        var tableInfo = MakeTableInfo(node.Arguments[0].Type);

                        Workflow(() =>
                        {
                            buildedSelect = true;

                            writer.Select();

                            if (isDistinct)
                            {
                                writer.Distinct();
                            }

                            WriteMembers(prefix, FilterMembers(tableInfo.ReadOrWrites));

                        }, Done);

                        break;
                    }

                    Done();

                    break;

                    void Done()
                    {
                        writer.From();

                        writer.OpenBrace();

                        using (var visitor = new CombinationVisitor(this))
                        {
                            visitor.Startup(node);
                        }

                        writer.CloseBrace();

                        writer.WhiteSpace();

                        writer.Name(prefix = GetEntryAlias(node.Arguments[0].Type, "x"));
                    }
                default:
                    base.VisitCore(node);

                    break;
            }

            ParameterExpression Join(Expression expression, bool isJoin)
            {
                switch (expression)
                {
                    case UnaryExpression unary:
                        return Join(unary.Operand, isJoin);
                    case LambdaExpression lambda when lambda.Parameters.Count == 1 || lambda.Parameters.Count == 2:
#if NETSTANDARD2_1_OR_GREATER
                        var parameter = lambda.Parameters[^1];
#else
                        var parameter = lambda.Parameters[lambda.Parameters.Count - 1];
#endif

                        if (isJoin)
                        {
                            var parameterType = TypeToUltimateType(parameter.Type);

                            if (!joinCache.TryGetValue(parameterType, out List<string> results))
                            {
                                joinCache.Add(parameterType, results = new List<string>());
                            }

                            results.Add(parameter.Name);
                        }
                        else
                        {
                            AnalysisAlias(parameter);
                        }
                        return parameter;
                    default:
                        throw new DSyntaxErrorException();
                }
            }
        }

        /// <inheritdoc />
        protected override void WriteTableName(Type paramererType)
        {
            if (buildTable)
            {
                base.WriteTableName(paramererType);
            }
        }

        /// <inheritdoc />
        protected override void WriteTableName(ITableInfo tableInfo, string alias)
        {
            if (buildTable)
            {
                base.WriteTableName(tableInfo, alias);
            }
        }

        /// <inheritdoc />
        protected override void VisitLinq(MethodCallExpression node)
        {
            if (useGroupBy && node.Arguments[0].IsGrouping())
            {
                VisitOfLinqGroupBy(node);
            }
            else
            {
                base.VisitLinq(node);
            }
        }

        /// <summary>
        /// 创建条件实列。
        /// </summary>
        /// <param name="baseVisitor">参数。</param>
        /// <returns></returns>
        public virtual SelectVisitor CreateInstance(BaseVisitor baseVisitor) => new SelectVisitor(baseVisitor);

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.IsQueryable())
            {
                if (buildSelect)
                {
                    if (useGroupBy)
                    {
                        throw new DSyntaxErrorException("使用“GroupBy”函数必须指定查询字段（例如：x=> x.Id 或 x=> new { x.Id, x.Name }）！");
                    }

                    buildSelect = buildFrom = false;
                    buildedSelect = true;

                    return Select(node);
                }

                if (buildFrom)
                {
                    buildFrom = false;

                    writer.From();

                    WriteTableName(node.Type);
                }
                else if (hasJoin)
                {
                    WriteTableName(node.Type);
                }

                return node;
            }

            return base.VisitConstant(node);
        }

        /// <inheritdoc />
        protected override void DefMemberBindingAs(MemberBinding member, Type memberOfHostType)
        {
            writer.As(GetMemberNaming(memberOfHostType, member.Member));
        }

        /// <inheritdoc />
        protected override void DefNewMemberAs(MemberInfo memberInfo, Type memberOfHostType)
        {
            writer.As(GetMemberNaming(memberOfHostType, memberInfo));
        }

        /// <inheritdoc />
#if NET40
        protected override ReadOnlyCollection<MemberBinding> FilterMemberBindings(ReadOnlyCollection<MemberBinding> bindings)
#else
        protected override IReadOnlyCollection<MemberBinding> FilterMemberBindings(IReadOnlyCollection<MemberBinding> bindings)
#endif
        {
            var vbindings = base.FilterMemberBindings(bindings);

            if (useCast)
            {
                return vbindings
                .Where(x => memberFilters.Contains(x.Member.Name.ToLower()))
                .ToList()
#if NET40
                .AsReadOnly()
#endif
                ;
            }

            return vbindings;
        }

        /// <inheritdoc />
#if NET40
        protected override IDictionary<string, string> FilterMembers(IDictionary<string, string> members)
#else
        protected override IReadOnlyDictionary<string, string> FilterMembers(IReadOnlyDictionary<string, string> members)
#endif
        {
            if (useCast)
            {
                var dic = new Dictionary<string, string>();

                foreach (var kv in members)
                {
                    if (memberFilters.Contains(kv.Key.ToLower()))
                    {
                        dic.Add(kv.Key, kv.Value);
                    }
                }

                return base.FilterMembers(dic);
            }

            return base.FilterMembers(members);
        }

        /// <inheritdoc />
        protected override Type TypeToUltimateType(Type entryType)
        {
            var ultimateType = base.TypeToUltimateType(entryType);

            if (castCache.TryGetValue(ultimateType, out Type ultimateCastType))
            {
                return ultimateCastType;
            }

            return ultimateType;
        }

        /// <inheritdoc />
        protected override void VisitMemberLeavesIsObject(MemberExpression node)
        {
            if (node.Expression.IsGrouping())
            {
                if (inSelect)
                {
                    throw new DSyntaxErrorException("查询结果不支持导航属性!");
                }

                bool flag = false;

                foreach (var kv in groupByExpressions)
                {
                    if (flag)
                    {
                        writer.Delimiter();
                    }
                    else
                    {
                        flag = true;
                    }

                    byVisitor.Visit(kv.Value);
                }
            }
            else
            {
                base.VisitMemberLeavesIsObject(node);
            }
        }

        /// <summary>
        /// 查询的默认值表达式。
        /// <see cref="Queryable.SelectMany{TSource, TCollection, TResult}(IQueryable{TSource}, Expression{Func{TSource, IEnumerable{TCollection}}}, Expression{Func{TSource, TCollection, TResult}})"/>
        /// <seealso cref="Enumerable.DefaultIfEmpty{TSource}(IEnumerable{TSource}, TSource)"/>
        /// </summary>
        protected virtual void VisitSelectManyDefaultExpression(MemberExpression member, Expression node)
        {
            switch (node)
            {
                case ConstantExpression constant:
                    switch (member.Member.MemberType)
                    {
                        case MemberTypes.Field when member.Member is FieldInfo field:
                            writer.Parameter(member.Member.Name.ToUrlCase(), field.GetValue(constant.Value));
                            break;
                        case MemberTypes.Property when member.Member is PropertyInfo property:
                            writer.Parameter(member.Member.Name.ToUrlCase(), property.GetValue(constant.Value, null));
                            break;
                        default:
                            throw new DSyntaxErrorException();
                    }
                    break;
                case MemberExpression memberExpression when IsPlainVariable(memberExpression):

                    var objValue = memberExpression.GetValueFromExpression();

                    switch (member.Member.MemberType)
                    {
                        case MemberTypes.Field when member.Member is FieldInfo field:
                            writer.Parameter(member.Member.Name.ToUrlCase(), field.GetValue(objValue));
                            break;
                        case MemberTypes.Property when member.Member is PropertyInfo property:
                            writer.Parameter(member.Member.Name.ToUrlCase(), property.GetValue(objValue, null));
                            break;
                        default:
                            throw new DSyntaxErrorException();
                    }
                    break;
                case MemberInitExpression memberInit:
                    foreach (var binding in memberInit.Bindings)
                    {
                        if (binding.Member != member.Member)
                        {
                            continue;
                        }

                        VisitMemberBinding(binding);

                        goto label_break;
                    }

                    throw new DSyntaxErrorException($"成员“{member.Member.Name}”未设置默认值!");

                    label_break:

                    break;

                case NewExpression newExpression:
                    Visit(newExpression.Arguments[newExpression.Members.IndexOf(member.Member)]);
                    break;
                default:
                    throw new DSyntaxErrorException();
            }
        }

        private void Aw_VisitSelectManyDefaultExpression(string parameterName, MemberExpression member, Expression node, Action action)
        {
            var tableInfo = MakeTableInfo(member.Expression.Type);

            var prefix = GetEntryAlias(tableInfo.TableType, parameterName);

            writer.OpenBrace();
            writer.Write("CASE WHEN ");

            bool flag = false;

            if (tableInfo.Keys.Count > 0)
            {
                foreach (var item in tableInfo.ReadOrWrites.Where(x => tableInfo.Keys.Contains(x.Key)))
                {
                    if (flag)
                    {
                        writer.Or();
                    }
                    else
                    {
                        flag = true;
                    }

                    writer.NameDot(prefix, item.Value);

                    writer.Write(" IS NOT NULL");
                }
            }
            else
            {
                foreach (var item in tableInfo.ReadOrWrites)
                {
                    if (flag)
                    {
                        writer.Or();
                    }
                    else
                    {
                        flag = true;
                    }

                    writer.NameDot(prefix, item.Value);

                    writer.Write(" IS NOT NULL");
                }
            }

            writer.Write(" THEN ");

            action.Invoke();

            writer.Write(" ELSE ");

            VisitSelectManyDefaultExpression(member, node);

            writer.Write(" END");
            writer.CloseBrace();
        }

        /// <inheritdoc />
        protected override void VisitMemberIsDependOnParameterWithMemmberExpression(MemberExpression member, MemberExpression node)
        {
            if (defaultCache.TryGetValue(Tuple.Create(member.Type, member.Member.Name), out Expression expression))
            {
                Aw_VisitSelectManyDefaultExpression(member.Member.Name, node, expression, () => base.VisitMemberIsDependOnParameterWithMemmberExpression(member, node));
            }
            else
            {
                base.VisitMemberIsDependOnParameterWithMemmberExpression(member, node);
            }
        }

        /// <summary>
        /// 成员依赖于参数成员的参数成员。
        /// </summary>
        /// <param name="parameter">等同于<paramref name="node"/>.Expression</param>
        /// <param name="node">节点。</param>
        protected override void VisitMemberIsDependOnParameterWithParameterExpression(ParameterExpression parameter, MemberExpression node)
        {
            if (defaultCache.TryGetValue(Tuple.Create(parameter.Type, parameter.Name), out Expression expression))
            {
                Aw_VisitSelectManyDefaultExpression(parameter.Name, node, expression, () => base.VisitMemberIsDependOnParameterWithParameterExpression(parameter, node));
            }
            else
            {
                base.VisitMemberIsDependOnParameterWithParameterExpression(parameter, node);
            }
        }

        /// <inheritdoc />
        protected override void VisitMemberIsDependOnParameterTypeIsPlain(MemberExpression node)
        {
            if (useGroupByAggregation)
            {
                base.VisitMemberIsDependOnParameterTypeIsPlain(node);
            }
            else if (node.Expression.IsGrouping())
            {
                if (groupByExpressions.TryGetValue(node.Member, out Expression expression))
                {
                    byVisitor.Visit(expression);
                }
                else if (node.Member.Name == "Key" && groupByExpressions.TryGetValue(GroupByVisitor.KeyMember, out expression))
                {
                    byVisitor.Visit(expression);
                }
                else
                {
                    throw new DSyntaxErrorException();
                }
            }
            else
            {
                base.VisitMemberIsDependOnParameterTypeIsPlain(node);
            }
        }

        /// <inheritdoc />

        protected override void VisitMemberIsDependOnParameterTypeIsObject(MemberExpression node)
        {
            if (!node.Expression.IsGrouping())
            {
                base.VisitMemberIsDependOnParameterTypeIsObject(node);
            }
        }

        /// <inheritdoc />
        protected virtual void VisitOfLinqGroupBy(MethodCallExpression node)
        {
            using (var visitor = new GroupByLinqVisitor(this))
            {
                useGroupByAggregation = true;
                visitor.Startup(node);
                useGroupByAggregation = false;
            }
        }

        /// <inheritdoc />
        protected override bool TryGetEntryAlias(Type entryType, string parameterName, bool check, out string aliasName)
        {
            if (!hasJoin || parameterName.IsEmpty() || !joinCache.TryGetValue(entryType, out List<string> list) || !list.Contains(parameterName))
            {
                return base.TryGetEntryAlias(entryType, parameterName, check, out aliasName);
            }

            aliasName = parameterName;

            return true;
        }

        /// <inheritdoc />
        protected override void VisitParameterLeavesIsObject(ParameterExpression node)
        {
            if (inSelect)
            {
                base.VisitParameterLeavesIsObject(node);
            }
            else
            {
                throw new DSyntaxErrorException("不允许使用表达式参数作为的排序、分组等的条件！");
            }
        }

#region SELECT
        /// <summary>
        /// Select。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual Expression Select(ConstantExpression node)
        {
            var parameterType = TypeToUltimateType(node.Type);

            var prefix = GetEntryAlias(parameterType, string.Empty);

            var tableInfo = MakeTableInfo(parameterType);

            writer.Select();

            if (isDistinct && !isExists)
            {
                writer.Distinct();
            }

            var members = FilterMembers(tableInfo.ReadOrWrites);

            if (isExists)
            {
                var existsMembers = new List<KeyValuePair<string, string>>();

                if (tableInfo.Keys.Count > 0)
                {
                    foreach (var item in members)
                    {
                        if (tableInfo.Keys.Contains(item.Key))
                        {
                            existsMembers.Add(item);
                        }
                    }
                }

                if (existsMembers.Count == tableInfo.Keys.Count)
                {
                    WriteMembers(prefix, existsMembers);
                }
                else
                {
                    WriteMembers(prefix, members);
                }
            }
            else
            {
                WriteMembers(prefix, members);
            }

            writer.From();

            WriteTableName(tableInfo, prefix);

            return node;
        }
#endregion

#region SQL
        /// <summary>
        /// SQL
        /// </summary>
        /// <returns></returns>
        public override string ToSQL() => writer.ToSQL(take, skip);
#endregion

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                memberFilters.Clear();
                castCache.Clear();
                groupByExpressions.Clear();

                if (hasJoin)
                {
                    joinCache.Clear();
                }

                if (useGroupBy)
                {
                    byVisitor.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}

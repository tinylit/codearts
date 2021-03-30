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

namespace CodeArts.Db.Lts.Visitors
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
        /// 是否构建 SELECT 部分。
        /// </summary>
        private bool buildSelect = true;

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
        private List<string> MemberFilters = new List<string>();

        /// <summary>
        /// 类型关系。
        /// </summary>
        private readonly Dictionary<Type, Type> CastCache = new Dictionary<Type, Type>();

        /// <summary>
        /// 分组字段集合。
        /// </summary>
        private readonly Dictionary<MemberInfo, string> GroupByFields = new Dictionary<MemberInfo, string>();

        /// <summary>
        /// 链接表别名。
        /// </summary>
        private readonly Dictionary<Type, List<string>> JoinCache = new Dictionary<Type, List<string>>();

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

                    base.Visit(node.Arguments[0]);

                    if (!useOrderBy && name == MethodCall.TakeLast)
                    {
                        throw new DSyntaxErrorException($"使用函数({name})时，必须使用排序函数(OrderBy/OrderByDescending)!");
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

                    if (this.take == -1)
                    {
                        this.take = take;
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
                    useAggregation = true;
                    using (var visitor = new MaxVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Min:
                    buildSelect = false;
                    useAggregation = true;
                    using (var visitor = new MinVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Sum:
                    buildSelect = false;
                    useAggregation = true;
                    using (var visitor = new SumVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Average:
                    buildSelect = false;
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

                    base.Visit(node.Arguments[0]);

                    if (!useOrderBy && name == MethodCall.SkipLast)
                    {
                        throw new DSyntaxErrorException($"使用函数({name})时，必须使用排序函数(OrderBy/OrderByDescending)!");
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
                        CastCache.Add(node.Type.GetGenericArguments().First(), TypeToEntryType(node.Arguments[0].Type));
                    }

                    if (!useGroupBy)
                    {
                        byVisitor = new GroupByVisitor(this, GroupByFields);
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

                    buildSelect = false;

                    writer.Select();

                    Workflow(() =>
                    {
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
                case MethodCall.Distinct:

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

                    if (!CastCache.ContainsKey(type))
                    {
                        CastCache.Add(type, TypeToEntryType(originalType));
                    }

                    var entry = TypeItem.Get(type);

                    if (MemberFilters.Count == 0)
                    {
                        MemberFilters.AddRange(entry.PropertyStores
                            .Where(x => x.CanRead && x.CanWrite)
                            .Select(x => x.Name.ToLower()));
                    }
                    else //? 取交集
                    {
                        MemberFilters = MemberFilters
                           .Intersect(entry.PropertyStores
                           .Where(x => x.CanRead && x.CanWrite)
                           .Select(x => x.Name.ToLower()))
                           .ToList();
                    }

                    if (MemberFilters.Count == 0)
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

                    void Join(Expression expression, bool isJoin)
                    {
                        if (expression is UnaryExpression unary && unary.Operand is LambdaExpression lambda && lambda.Parameters.Count == 1)
                        {
                            var parameter = lambda.Parameters[0];

                            if (isJoin)
                            {
                                var parameterType = TypeToUltimateType(parameter.Type);

                                if (!JoinCache.TryGetValue(parameterType, out List<string> results))
                                {
                                    JoinCache.Add(parameterType, results = new List<string>());
                                }

                                results.Add(parameter.Name);
                            }
                            else
                            {
                                AnalysisAlias(parameter);
                            }
                        }
                    }

                    Join(node.Arguments[2], false);

                    Join(node.Arguments[3], true);

                    using (var visitor = new JoinVisitor(this))
                    {
                        visitor.Startup(node);
                    }

                    buildTable = false;
                    break;
                case MethodCall.Any:
                    using (var visitor = new AnyVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.All:
                    using (var visitor = new AllVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Union:
                case MethodCall.Concat:
                case MethodCall.Except:
                case MethodCall.Intersect:

                    buildTable = false;
                    hasCombination = true;

                    if (isNoPackage)
                    {
                        buildSelect = false;

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
                            writer.Select();

                            if (isDistinct)
                            {
                                writer.Distinct();
                            }

                            WriteMembers(prefix, FilterMembers(tableInfo.ReadOrWrites));

                            writer.From();

                        }, Done);

                        break;
                    }

                    Done();

                    break;

                    void Done()
                    {
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

        private static bool IsGrouping(Expression node)
        {
            switch (node)
            {
                case ParameterExpression parameter:
                    return parameter.IsGrouping();
                case MethodCallExpression method:
                    return IsGrouping(method.Arguments[0]);
                default:
                    return false;
            }
        }

        /// <inheritdoc />
        protected override void VisitLinq(MethodCallExpression node)
        {
            if (useGroupBy && IsGrouping(node.Arguments[0]))
            {
                VisitOfLinqGroupBy(node);
            }
            else
            {
                base.VisitLinq(node);
            }
        }

        /// <summary>
        /// 创建实列。
        /// </summary>
        /// <param name="baseVisitor">参数。</param>
        /// <returns></returns>
        public virtual SelectVisitor CreateInstance(BaseVisitor baseVisitor) => new SelectVisitor(baseVisitor);

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (buildSelect && node.Type.IsQueryable())
            {
                if (useGroupBy)
                {
                    throw new DSyntaxErrorException("使用“GroupBy”函数必须指定查询字段（例如：x=> x.Id 或 x=> new { x.Id, x.Name }）！");
                }

                buildSelect = false;

                return Select(node);
            }

            if (hasJoin && node.Type.IsQueryable())
            {
                WriteTableName(node.Type);

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
                .Where(x => MemberFilters.Contains(x.Member.Name.ToLower()))
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
                    if (MemberFilters.Contains(kv.Key.ToLower()))
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

            if (CastCache.TryGetValue(ultimateType, out Type ultimateCastType))
            {
                return ultimateCastType;
            }

            return ultimateType;
        }

        /// <inheritdoc />
        protected override void VisitMemberLeavesIsObject(MemberExpression node)
        {
            if (node.Member.Name == "Key" && node.Expression.IsGrouping())
            {
                bool flag = false;

                foreach (var kv in GroupByFields)
                {
                    if (flag)
                    {
                        writer.Delimiter();
                    }
                    else
                    {
                        flag = true;
                    }

                    writer.Write(kv.Value);

                    if (inSelect)
                    {
                        writer.As("__key____" + kv.Key.Name.ToLower());
                    }
                }
            }
            else
            {
                base.VisitMemberLeavesIsObject(node);
            }
        }

        /// <inheritdoc />
        protected override void VisitMemberIsDependOnParameterTypeIsPlain(MemberExpression node)
        {
            if (useGroupByAggregation)
            {
                base.VisitMemberIsDependOnParameterTypeIsPlain(node);

                return;
            }

            if (GroupByFields.TryGetValue(node.Member, out string value))
            {
                writer.Write(value);

                return;
            }

            if (node.Member.Name == "Key" && node.Expression.IsGrouping())
            {
                if (GroupByFields.TryGetValue(GroupByVisitor.KeyMember, out value))
                {
                    writer.Write(value);

                    return;
                }
            }

            base.VisitMemberIsDependOnParameterTypeIsPlain(node);
        }

        /// <inheritdoc />

        protected override void VisitMemberIsDependOnParameterTypeIsObject(MemberExpression node)
        {
            if (node.Member.Name == "Key" && node.Expression.IsGrouping())
            {
                return;
            }

            base.VisitMemberIsDependOnParameterTypeIsObject(node);
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
            if (!hasJoin || parameterName.IsEmpty() || !JoinCache.TryGetValue(entryType, out List<string> list) || !list.Contains(parameterName))
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

            if (isDistinct)
            {
                writer.Distinct();
            }

            WriteMembers(prefix, FilterMembers(tableInfo.ReadOrWrites));

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
                MemberFilters.Clear();
                CastCache.Clear();
                GroupByFields.Clear();

                if (hasJoin)
                {
                    JoinCache.Clear();
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

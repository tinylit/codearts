using CodeArts.ORM.Exceptions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// Select。
    /// </summary>
    public class SelectVisitor : BaseVisitor
    {
        #region 匿名内部类。
        /// <summary>
        /// 智能开关
        /// </summary>
        private class OrderBySwitch
        {
            public OrderBySwitch(Action firstAction, Action unFirstAction)
            {
                FirstAction = firstAction;
                UnFirstAction = unFirstAction;
            }

            private bool isFirst = true;

            public Action FirstAction { get; }

            public Action UnFirstAction { get; }

            public void UnWrap(Action action)
            {
                bool isFirst = this.isFirst;

                this.isFirst = true;

                action?.Invoke();

                this.isFirst = isFirst;
            }

            public void OrderBy()
            {
                if (isFirst)
                {
                    isFirst = false;

                    FirstAction.Invoke();
                }
                else
                {
                    UnFirstAction.Invoke();
                }
            }
        }
        #endregion

        private int take = -1;

        private int skip = -1;

        private int selectDepth = 0;

        /// <summary>
        /// 条件。
        /// </summary>
        private readonly OrderBySwitch orderBySwitch;

        private bool buildSelect = true;

        /// <summary>
        /// 去重。
        /// </summary>
        private bool isDistinct = false;

        /// <summary>
        /// 逆序。
        /// </summary>
        internal bool reverseOrder = false;

        /// <summary>
        /// 使用了Count函数。
        /// </summary>
        private bool hasCount = false;

        /// <summary>
        /// 含聚合函数。
        /// </summary>
        private bool hasAggregation = false;

        /// <summary>
        /// 有设置排序。
        /// </summary>
        private bool hasOrderBy = false;

        /// <summary>
        /// 含组合函数。
        /// </summary>
        private bool hasCombination = false;

        /// <summary>
        /// 含Join函数。
        /// </summary>
        private bool hasJoin = false;

        private readonly ConcurrentDictionary<Type, List<string>> JoinCache = new ConcurrentDictionary<Type, List<string>>();

        /// <summary>
        /// inherit。
        /// </summary>
        public SelectVisitor(BaseVisitor visitor) : base(visitor, true)
        {
            orderBySwitch = new OrderBySwitch(writer.OrderBy, writer.Delimiter);
        }
        /// <summary>
        /// inherit。
        /// </summary>
        public SelectVisitor(ISQLCorrectSettings settings) : base(settings)
        {
            orderBySwitch = new OrderBySwitch(writer.OrderBy, writer.Delimiter);
        }

        /// <summary>
        /// inherit。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression StartupCore(MethodCallExpression node)
        {
            return UsingSelect(node, base.StartupCore);
        }

        private Expression UsingSelect(MethodCallExpression node, Func<MethodCallExpression, Expression> work)
        {
            Action action = null;

            Expression result = null;

            base.Workflow(() =>
            {
                if (buildSelect && !(node.Method.Name == MethodCall.Union ||
                node.Method.Name == MethodCall.Concat ||
                node.Method.Name == MethodCall.Except ||
                node.Method.Name == MethodCall.Intersect))
                {
                    action = Select(node);
                }

            }, () => result = work.Invoke(node));

            action?.Invoke();

            return result;
        }

        private Expression UsingMustSelect(MethodCallExpression node, Func<MethodCallExpression, Expression> work)
        {
            Action action = null;

            Expression result = null;

            buildSelect = false;

            base.Workflow(() =>
            {
                action = Select(node);
            }, () => result = work.Invoke(node));

            action?.Invoke();

            return result;
        }

        /// <summary>
        /// inherit。
        /// </summary>
        /// <returns></returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (hasCombination)
            {
                return node;
            }

            return base.VisitParameter(node);
        }

        /// <summary>
        /// inherit。
        /// </summary>
        /// <returns></returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.IsQueryable())
            {
                if (buildSelect)
                {
                    buildSelect = false;

                    return Select(node);
                }

                if (hasJoin)
                {
                    var tableInfo = base.MakeTableInfo(node.Type);

                    var prefix = base.GetEntryAlias(tableInfo.TableType, string.Empty);

                    writer.TableName(GetTableName(tableInfo), prefix);
                }

                return node;
            }

            return base.VisitConstant(node);
        }

        /// <summary>
        /// inherit。
        /// </summary>
        /// <returns></returns>
        protected internal override bool TryGetEntryAlias(Type entryType, string parameterName, bool check, out string aliasName)
        {
            if (hasJoin && parameterName.IsNotEmpty() && JoinCache.TryGetValue(entryType, out List<string> list) && list.Contains(parameterName))
            {
                aliasName = parameterName;

                return true;
            }

            return base.TryGetEntryAlias(entryType, parameterName, check, out aliasName);
        }

        /// <summary>
        /// 写入成员。
        /// </summary>
        /// <param name="aggregationName">聚合函数名称。</param>
        /// <param name="prefix">前缀。</param>
        /// <param name="field">字段。</param>
        /// <param name="alias">别名。</param>
        protected virtual void WriteMember(string aggregationName, string prefix, string field, string alias)
        {
            writer.Write(aggregationName);
            writer.OpenBrace();
            writer.Name(prefix, field);
            writer.CloseBrace();
        }

        /// <summary>
        /// 写入指定成员
        /// </summary>
        /// <param name="aggregationName">聚合函数名称。</param>
        /// <param name="prefix">前缀</param>
        /// <param name="names">成员集合</param>
        protected virtual void WriteMembers(string aggregationName, string prefix, IEnumerable<KeyValuePair<string, string>> names)
        {
            var kv = names.GetEnumerator();

            if (kv.MoveNext())
            {
                WriteMember(aggregationName, prefix, kv.Current.Value, kv.Current.Key);

                while (kv.MoveNext())
                {
                    writer.Delimiter();

                    WriteMember(aggregationName, prefix, kv.Current.Value, kv.Current.Key);
                }
            }
        }

        /// <summary>
        /// Select。
        /// </summary>
        /// <param name="node"></param>
        protected virtual Expression Select(ConstantExpression node)
        {
            var parameterType = TypeToUltimateType(node.Type);

            var prefix = base.GetEntryAlias(parameterType, string.Empty);

            var tableInfo = base.MakeTableInfo(parameterType);

            writer.Select();

            if (isDistinct)
            {
                writer.Distinct();
            }

            WriteMembers(prefix, FilterMembers(tableInfo.ReadOrWrites));

            writer.From();

            writer.TableName(GetTableName(tableInfo), prefix);

            return node;
        }

        /// <summary>
        /// Select。
        /// </summary>
        /// <param name="node"></param>
        protected virtual Action Select(MethodCallExpression node)
        {
            string name = node.Method.Name;

            writer.Select();

            if (isDistinct)
            {
                writer.Distinct();
            }

            string prefix;

            Expression parameterExp = node.Arguments[0];

            var tableInfo = base.MakeTableInfo(parameterExp.Type);

            var members = FilterMembers(tableInfo.ReadOrWrites);

            switch (name)
            {
                case MethodCall.LongCount:
                    name = "Count";
                    goto case MethodCall.Count;
                case MethodCall.Count:

                    writer.Write(name);

                    writer.OpenBrace();

                    prefix = base.GetEntryAlias(tableInfo.TableType, string.Empty);

                    if (tableInfo.Keys.Count == 1)
                    {
                        foreach (var kv in tableInfo.ReadOrWrites)
                        {
                            if (tableInfo.Keys.Contains(kv.Key))
                            {
                                writer.Name(prefix, kv.Value);

                                break;
                            }
                        }
                    }
                    else
                    {
                        writer.Write("1");
                    }

                    writer.CloseBrace();
                    break;

                case MethodCall.Average:
                    name = "Avg";
                    goto case MethodCall.Sum;
                case MethodCall.Max:
                case MethodCall.Min:
                case MethodCall.Sum:
                    if (node.Arguments.Count > 1)
                    {
                        writer.Write(name);

                        writer.OpenBrace();

                        base.Visit(node.Arguments[1]);

                        writer.CloseBrace();

                        prefix = base.GetEntryAlias(tableInfo.TableType, string.Empty);
                    }
                    else
                    {
                        prefix = base.GetEntryAlias(tableInfo.TableType, string.Empty);

                        WriteMembers(name, prefix, members);
                    }
                    break;
                case MethodCall.Any:
                case MethodCall.All:
                    return default;
                case MethodCall.Select:

                    base.Visit(node.Arguments[1]);

                    prefix = base.GetEntryAlias(tableInfo.TableType, string.Empty);
                    break;
                default:
                    prefix = base.GetEntryAlias(tableInfo.TableType, string.Empty);

                    WriteMembers(prefix, members);
                    break;
            }

            writer.From();

            if (hasCombination)
            {
                writer.OpenBrace();

                return () =>
                {
                    writer.CloseBrace();
                    writer.WhiteSpace();

                    writer.Name(prefix.IsEmpty() ? "CTE" : prefix);
                };
            }

            if (!hasJoin)
            {
                writer.TableName(GetTableName(tableInfo), prefix);
            }

            return default;
        }

        /// <summary>
        /// inherit。
        /// </summary>
        protected override Expression VisitOfQueryable(MethodCallExpression node)
        {
            string name = node.Method.Name;

            if (node.Arguments.Count > 1 ?
                !(name == MethodCall.Take || name == MethodCall.Skip || name == MethodCall.TakeLast || name == MethodCall.SkipLast || name == MethodCall.DefaultIfEmpty || name == MethodCall.ElementAt || name == MethodCall.ElementAtOrDefault)
                :
                (name == MethodCall.Sum || name == MethodCall.Max || name == MethodCall.Min || name == MethodCall.Average)
                )
            {
                selectDepth++;
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

                    return node;
                case MethodCall.Take:
                case MethodCall.TakeLast:

                    if (hasAggregation)
                    {
                        throw new DSyntaxErrorException($"使用聚合函数时，禁止使用分页函数({name})!");
                    }

                    if (name == MethodCall.TakeLast)
                    {
                        reverseOrder ^= true;
                    }

                    base.Visit(node.Arguments[0]);

                    if (!hasOrderBy && name == MethodCall.TakeLast)
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

                    return node;
                case MethodCall.First:
                case MethodCall.FirstOrDefault:
                case MethodCall.Single:
                case MethodCall.SingleOrDefault:

                    // TOP(1)
                    this.take = 1;

                    if (node.Arguments.Count > 1)
                    {
                        return VisitCondition(node);
                    }

                    base.Visit(node.Arguments[0]);

                    return node;
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

                    if (!hasOrderBy)
                    {
                        throw new DSyntaxErrorException($"使用函数({name})时，必须使用排序函数(OrderBy/OrderByDescending)!");
                    }

                    return node;
                case MethodCall.Max:
                case MethodCall.Min:
                case MethodCall.Sum:
                case MethodCall.Average:
                case MethodCall.Count:
                case MethodCall.LongCount:

                    buildSelect = false;

                    hasAggregation = true;

                    hasCount = name == MethodCall.Count || name == MethodCall.LongCount;

                    return VisitAggregation(node);
                case MethodCall.Skip:
                case MethodCall.SkipLast:

                    if (hasAggregation)
                    {
                        throw new DSyntaxErrorException($"使用聚合函数时，禁止使用分页函数({name})!");
                    }

                    if (name == MethodCall.SkipLast)
                    {
                        reverseOrder ^= true;
                    }

                    base.Visit(node.Arguments[0]);

                    if (!hasOrderBy && name == MethodCall.SkipLast)
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

                    return node;
                case MethodCall.Reverse:

                    reverseOrder ^= true;

                    base.Visit(node.Arguments[0]);

                    if (!hasOrderBy)
                    {
                        throw new DSyntaxErrorException($"使用函数“{name}”时，必须使用排序函数(OrderBy/OrderByDescending)!");
                    }

                    return node;
                case MethodCall.Select:

                    if (selectDepth > 1)
                    {
                        throw new DSyntaxErrorException($"请将函数“{name}”置于查询最后一个包含入参的函数之后!");
                    }

                    if (hasCount)
                    {
                        return base.Visit(node.Arguments[0]);
                    }

                    buildSelect = false;

                    return UsingMustSelect(node, arg => base.Visit(arg.Arguments[0]));
                case MethodCall.Distinct:

                    isDistinct = true;

                    return base.Visit(node.Arguments[0]);
                case MethodCall.OrderBy:
                case MethodCall.ThenBy:
                case MethodCall.OrderByDescending:
                case MethodCall.ThenByDescending:

                    hasOrderBy = true;

                    if (hasAggregation)
                    {
                        return base.Visit(node.Arguments[0]);
                    }

                    if (buildSelect)
                    {
                        buildSelect = false;

                        Done(node);

                        buildSelect = true;

                        return node;
                    }

                    Done(node);

                    return node;

                    void Done(MethodCallExpression nodeExp)
                    {
                        bool thatReverseOrder = reverseOrder;

                        Workflow(() => writer.UsingSort(() =>
                        {
                            orderBySwitch.OrderBy();

                            base.Visit(nodeExp.Arguments[1]);

                            if (thatReverseOrder ^ nodeExp.Method.Name.EndsWith("Descending"))
                            {
                                writer.Descending();
                            }

                        }), () => base.Visit(nodeExp.Arguments[0]));

                    }
                case MethodCall.Join:

                    hasJoin = true;

                    void Join(Expression expression, bool isJoin)
                    {
                        if (expression is UnaryExpression unary && unary.Operand is LambdaExpression lambda && lambda.Parameters.Count == 1)
                        {
                            var parameter = lambda.Parameters[0];

                            var parameterType = TypeToUltimateType(parameter.Type);

                            if (isJoin)
                            {
                                JoinCache.GetOrAdd(parameterType, _ => new List<string>())
                                    .Add(parameter.Name);
                            }
                            else
                            {
                                AliasCache.AddOrUpdate(parameterType, parameter.Name, (_, _2) => parameter.Name);
                            }
                        }
                    }

                    Join(node.Arguments[2], false);

                    Join(node.Arguments[3], true);

                    return VisitJoin(node);
                default:
                    return base.VisitOfQueryable(node);
            }
        }

        /// <summary>
        /// inherit。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitJoin(MethodCallExpression node)
        {
            using (var visitor = new JoinVisitor(this))
            {
                visitor.Startup(node);
            }

            return node;
        }

        /// <summary>
        /// inherit。
        /// </summary>
        /// <returns></returns>
        protected override Expression VisitCombination(MethodCallExpression node)
        {
            try
            {
                hasCombination = false;

                return base.VisitCombination(node);
            }
            finally
            {
                hasCombination = true;
            }
        }

        /// <summary>
        /// 聚合函数。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitAggregation(MethodCallExpression node)
        {
            string name = node.Method.Name;

            if (name == MethodCall.Count || name == MethodCall.LongCount)
            {
                return VisitCount(node);
            }

            if (node.Arguments.Count > 1)
            {
                return VisitAggregationMultiArgs(node);
            }

            return VisitAggregationSingleArg(node);
        }

        /// <summary>
        /// 聚合函数之多参数。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitAggregationMultiArgs(MethodCallExpression node)
        {
            return UsingMustSelect(node, arg => base.Visit(arg.Arguments[0]));
        }

        /// <summary>
        /// 聚合函数之单参数。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitAggregationSingleArg(MethodCallExpression node)
        {
            return UsingMustSelect(node, arg => base.Visit(arg.Arguments[0]));
        }

        /// <summary>
        /// 聚合“Count”或“LongCount”函数。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitCount(MethodCallExpression node)
        {
            return UsingMustSelect(node, arg =>
            {
                if (arg.Arguments.Count > 1)
                {
                    return base.VisitCondition(node);
                }

                return base.Visit(arg.Arguments[0]);
            });
        }

        /// <summary>
        /// 过滤成员
        /// </summary>
        /// <param name="members">成员集合</param>
        /// <returns></returns>
        protected virtual ReadOnlyCollection<MemberInfo> FilterMembers(ReadOnlyCollection<MemberInfo> members) => members;

        /// <summary>
        /// 访问 new 成员。
        /// </summary>
        /// <param name="memberInfo">成员信息。</param>
        /// <param name="memberExp">成员表达式。</param>
        /// <param name="memberOfHostType">成员所在宿主类型。</param>
        /// <returns></returns>
        protected virtual void VisitNewMember(MemberInfo memberInfo, Expression memberExp, Type memberOfHostType)
        {
            VisitCheckIfSubconnection(memberExp);
        }

        /// <summary>
        /// inherit。
        /// </summary>
        /// <returns></returns>
        protected override Expression VisitNew(NewExpression node)
        {
            var members = FilterMembers(node.Members);

            var enumerator = members.GetEnumerator();

            if (enumerator.MoveNext())
            {
                Type memberOfHostType = node.Type;

                VisitMyMember(enumerator.Current);

                while (enumerator.MoveNext())
                {
                    writer.Delimiter();

                    VisitMyMember(enumerator.Current);
                }

                void VisitMyMember(MemberInfo member)
                {
                    VisitNewMember(member, node.Arguments[node.Members.IndexOf(member)], memberOfHostType);
                }

                return node;
            }
            else
            {
                throw new DException("未指定查询字段!");
            }

        }

        #region SQL
        /// <summary>
        /// SQL
        /// </summary>
        /// <returns></returns>
        public override string ToSQL() => writer.ToSQL(take, skip);
        #endregion
    }
}

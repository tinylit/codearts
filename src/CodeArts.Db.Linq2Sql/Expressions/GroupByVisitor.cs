using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.Db.Expressions
{
    /// <summary>
    /// <see cref="Queryable.GroupBy{TSource, TKey}(IQueryable{TSource}, Expression{Func{TSource, TKey}})"/>
    /// </summary>
    public class GroupByVisitor : CoreVisitor
    {
        internal class MyMemberExpression : Expression
        {
            public MemberInfo Member { get; }
            public Expression Expression { get; }

            public MyMemberExpression(MemberInfo memberInfo, Expression node)
            {
                Member = memberInfo;
                Expression = node;
            }
        }

        /// <summary>
        /// 使用了GROUP BY 聚合函数。
        /// </summary>
        private bool useGroupByAggregation = false;

        private volatile bool initGroupVisitor = true;

        private readonly SelectVisitor visitor;
        private readonly Dictionary<Tuple<Type, string>, Expression> defaultCache;
        private readonly Dictionary<MemberInfo, Expression> groupByExpressions;

        /// <summary>
        /// <see cref="IGrouping{TKey, TElement}.Key"/>
        /// </summary>
        public static readonly MemberInfo KeyMember = typeof(IGrouping<,>).GetProperty("Key");

        /// <inheritdoc />
        public GroupByVisitor(SelectVisitor visitor, Dictionary<Tuple<Type, string>, Expression> defaultCache, Dictionary<MemberInfo, Expression> groupByExpressions) : base(visitor, false, ConditionType.Having)
        {
            this.visitor = visitor;
            this.defaultCache = defaultCache;
            this.groupByExpressions = groupByExpressions;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node)
            => node.Method.DeclaringType == Types.Queryable;

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            if (node.Method.Name == MethodCall.GroupBy)
            {
                int startIndex = 0, removeLength = 0;

                Workflow(() =>
                {
                    visitor.Visit(node.Arguments[0]);

                    startIndex = writer.AppendAt > -1 ? writer.AppendAt : writer.Length;

                }, () =>
                {
                    startIndex = writer.Length;

                    writer.GroupBy();

                    VisitGroupBy(node.Arguments[1]);

                    removeLength = writer.Length - startIndex;
                });

                initGroupVisitor = false;

                //? GroupBy 总是先行，遇到默认值是，重新生成GroupBy，以确保默认值也纳入分组结果。
                if (defaultCache.Count > 0)
                {
                    writer.Remove(startIndex, removeLength);

                    var index = writer.Length;
                    var length = writer.Length;
                    var appendAt = writer.AppendAt;

                    if (appendAt > -1)
                    {
                        index -= (index - appendAt);
                    }

                    writer.AppendAt = startIndex;

                    writer.GroupBy();

                    VisitGroupBy(node.Arguments[1]);

                    if (appendAt > -1)
                    {
                        appendAt += writer.Length - length;
                    }

                    writer.AppendAt = appendAt;
                }
            }
            else
            {
                base.StartupCore(node);
            }
        }

        /// <inheritdoc />
        protected override void VisitCore(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Where when node.Arguments[0].Type.IsGroupingQueryable():
                case MethodCall.TakeWhile when node.Arguments[0].Type.IsGroupingQueryable():
                case MethodCall.SkipWhile when node.Arguments[0].Type.IsGroupingQueryable():
                    base.VisitCore(node);
                    break;
                default:
                    visitor.Visit(node);
                    break;
            }
        }

        /// <inheritdoc />
        public override Expression Visit(Expression node)
        {
            if (node is MyMemberExpression memberExpression)
            {
                base.VisitNewMember(memberExpression.Member, memberExpression.Expression);

                return node;
            }

            return base.Visit(node);
        }

        /// <inheritdoc />
        protected override void VisitLinq(MethodCallExpression node)
        {
            if (node.Arguments[0].IsGrouping())
            {
                VisitOfLinqGroupBy(node);
            }
            else
            {
                base.VisitLinq(node);
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
        protected override void VisitMemberLeavesIsObject(MemberExpression node)
        {
            if (node.Expression.IsGrouping())
            {
                throw new DSyntaxErrorException("不支持导航属性!");
            }
            else
            {
                base.VisitMemberLeavesIsObject(node);
            }
        }

        /// <inheritdoc />
        protected override void VisitMemberIsDependOnParameterTypeIsPlain(MemberExpression node)
        {
            if (node.Expression.IsGrouping())
            {
                if (groupByExpressions.TryGetValue(node.Member, out Expression expression))
                {
                    Visit(expression);
                }
                else if (node.Member.Name == "Key" && groupByExpressions.TryGetValue(KeyMember, out expression))
                {
                    Visit(expression);
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
        protected internal override void VisitNewMember(MemberInfo memberInfo, Expression node)
        {
            if (initGroupVisitor)
            {
                groupByExpressions[memberInfo] = new MyMemberExpression(memberInfo, node);
            }

            base.VisitNewMember(memberInfo, node);
        }

        /// <inheritdoc />
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            if (initGroupVisitor)
            {
                groupByExpressions[node.Member] = node.Expression;
            }

            return base.VisitMemberAssignment(node);
        }

        /// <inheritdoc />
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (useGroupByAggregation)
            {
                return node;
            }

            return base.VisitParameter(node);
        }

        /// <inheritdoc />
        protected virtual void VisitGroupBy(Expression node)
        {
            switch (node)
            {
                case MemberExpression member:
                    if (initGroupVisitor)
                    {
                        groupByExpressions[KeyMember] = member;
                    }

                    base.VisitMember(member);
                    break;
                case NewExpression newExpression:
                    VisitNew(newExpression);
                    break;
                case MemberInitExpression memberInit:
                    VisitMemberInit(memberInit);
                    break;
                case LambdaExpression lambda:
                    VisitGroupBy(lambda.Body);
                    break;
                case UnaryExpression unary:
                    VisitGroupBy(unary.Operand);
                    break;
                case InvocationExpression invocation:
                    VisitGroupBy(invocation.Expression);
                    break;
                default:
                    throw new NotSupportedException();
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
        protected override void VisitIsBoolean(Expression node)
        {
            using (var visitor = new HavingVisitor(this, groupByExpressions))
            {
                visitor.Startup(node);
            }
        }
    }
}

using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.GroupBy{TSource, TKey}(IQueryable{TSource}, System.Linq.Expressions.Expression{Func{TSource, TKey}})"/>
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

        private readonly SelectVisitor visitor;
        private readonly Dictionary<MemberInfo, Expression> groupByExpressions;

        /// <summary>
        /// <see cref="IGrouping{TKey, TElement}.Key"/>
        /// </summary>
        public static readonly MemberInfo KeyMember = typeof(IGrouping<,>).GetProperty("Key");

        /// <inheritdoc />
        public GroupByVisitor(SelectVisitor visitor, Dictionary<MemberInfo, Expression> groupByExpressions) : base(visitor, false, ConditionType.Having)
        {
            this.visitor = visitor;
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
                Workflow(() => visitor.Visit(node.Arguments[0]), () =>
                {
                    writer.GroupBy();

                    VisitGroupBy(node.Arguments[1]);
                });
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

        /// <inheritdoc />
        protected override void VisitMemberLeavesIsObject(MemberExpression node)
        {
            if (node.Expression.IsGrouping())
            {
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

                    Visit(kv.Value);

                    writer.As("__key____" + kv.Key.Name.ToLower());
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
            groupByExpressions[memberInfo] = new MyMemberExpression(memberInfo, node);

            base.VisitNewMember(memberInfo, node);
        }

        /// <inheritdoc />
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            groupByExpressions[node.Member] = node.Expression;

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
                    groupByExpressions[KeyMember] = member;

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

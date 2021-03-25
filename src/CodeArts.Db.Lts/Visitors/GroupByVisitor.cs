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
        private readonly SelectVisitor visitor;
        private readonly Dictionary<MemberInfo, string> groupByRelationFields;

        /// <summary>
        /// <see cref="IGrouping{TKey, TElement}.Key"/>
        /// </summary>
        public static readonly MemberInfo KeyMember = typeof(IGrouping<,>).GetProperty("Key");

        /// <inheritdoc />
        public GroupByVisitor(SelectVisitor visitor, Dictionary<MemberInfo, string> groupByRelationFields) : base(visitor, false, ConditionType.Having)
        {
            this.visitor = visitor;
            this.groupByRelationFields = groupByRelationFields;
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
        protected override void VisitMemberLeavesIsObject(MemberExpression node)
        {
            if (node.Member.Name == "Key" && node.Expression.IsGrouping())
            {
                bool flag = false;

                foreach (var kv in groupByRelationFields)
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
                    writer.As("__key____" + kv.Key.Name.ToLower());
                }
            }
            else
            {
                base.VisitMemberLeavesIsObject(node);
            }
        }

        /// <summary>
        /// 成员依赖于参数成员（参数类型是值类型或字符串类型）。
        /// </summary>
        /// <returns></returns>
        protected override void VisitMemberIsDependOnParameterTypeIsPlain(MemberExpression node)
        {
            if (groupByRelationFields.TryGetValue(node.Member, out string value))
            {
                writer.Write(value);

                return;
            }

            if (node.Member.Name == "Key" && node.Expression.IsGrouping())
            {
                if (groupByRelationFields.TryGetValue(KeyMember, out value))
                {
                    writer.Write(value);

                    return;
                }
            }

            base.VisitMemberIsDependOnParameterTypeIsPlain(node);
        }


        private void GroupByRelation(MemberInfo memberInfo, Action actionVisit)
        {
            var index = writer.Length;
            var length = writer.Length;
            var appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                index -= (index - appendAt);
            }

            writer.AppendAt = index;

            actionVisit.Invoke();

            int offset = writer.Length - length;

            string colStr = writer.ToString(index, offset);

            if (appendAt > -1)
            {
                appendAt += offset;
            }

            writer.AppendAt = appendAt;

            groupByRelationFields[memberInfo] = colStr;
        }

        /// <inheritdoc />
        protected internal override void VisitNewMember(MemberInfo memberInfo, Expression node)
            => GroupByRelation(memberInfo, () => base.VisitNewMember(memberInfo, node));

        /// <inheritdoc />
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            MemberAssignment result = node;

            GroupByRelation(node.Member, () => result = base.VisitMemberAssignment(node));

            return result;
        }

        /// <inheritdoc />
        protected virtual void VisitGroupBy(Expression node)
        {
            switch (node)
            {
                case MemberExpression member:
                    GroupByRelation(KeyMember, () => base.VisitMember(member));
                    break;
                case NewExpression newExpression:
                    VisitNew(newExpression);
                    break;
                case ParameterExpression parameter:
                    var tableInfo = MakeTableInfo(parameter.Type);

                    var prefix = GetEntryAlias(tableInfo.TableType, parameter.Name);
                    var members = FilterMembers(tableInfo.ReadOrWrites);

                    WriteMembers(prefix, members);

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
    }
}

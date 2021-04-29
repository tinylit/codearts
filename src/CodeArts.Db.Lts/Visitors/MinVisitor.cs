using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.Min{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}})"/>
    /// </summary>
    public class MinVisitor : CoreVisitor
    {
        const string FnName = "MIN";

        private readonly SelectVisitor visitor;

        /// <inheritdoc />
        public MinVisitor(SelectVisitor visitor) : base(visitor, false)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node)
            => node.Method.Name == MethodCall.Min && node.Method.DeclaringType == Types.Queryable;

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            writer.Select();

            if (node.Arguments.Count == 2)
            {
                Workflow(() => VisitMin(node.Arguments[1]), () => visitor.Visit(node.Arguments[0]));
            }
            else
            {
                Workflow(() =>
                {
                    var tableInfo = MakeTableInfo(node.Arguments[0].Type);

                    var prefix = GetEntryAlias(tableInfo.TableType, string.Empty);

                    WriteMembers(prefix, FilterMembers(tableInfo.ReadOrWrites));

                }, () => visitor.Visit(node.Arguments[0]));
            }
        }

        /// <inheritdoc />
        protected internal override void VisitNewMember(MemberInfo memberInfo, Expression node)
        {
            writer.Write(FnName);
            writer.OpenBrace();

            base.VisitNewMember(memberInfo, node);

            writer.CloseBrace();
        }

        /// <inheritdoc />
        protected override void DefNewMemberAs(MemberInfo memberInfo, Type memberOfHostType)
        {
            writer.As(memberInfo.Name);
        }

        /// <inheritdoc />
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            writer.Write(FnName);
            writer.OpenBrace();

            visitor.Visit(node.Expression);

            writer.CloseBrace();

            return node;
        }

        /// <inheritdoc />
        protected override void DefMemberBindingAs(MemberBinding member, Type memberOfHostType)
        {
            writer.As(member.Member.Name);
        }

        /// <inheritdoc />
        protected override void WriteMember(string prefix, string field)
        {
            writer.Write(FnName);
            writer.OpenBrace();

            base.WriteMember(prefix, field);

            writer.CloseBrace();
        }

        /// <inheritdoc />
        protected override void DefMemberAs(string field, string alias)
        {
            writer.As(alias);
        }

        /// <inheritdoc />
        protected virtual void VisitMin(Expression node)
        {
            switch (node)
            {
                case MemberExpression member:
                    writer.Write(FnName);
                    writer.OpenBrace();

                    visitor.Visit(member);

                    writer.CloseBrace();
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
                    VisitMin(lambda.Body);
                    break;
                case UnaryExpression unary:
                    VisitMin(unary.Operand);
                    break;
                case InvocationExpression invocation:
                    VisitMin(invocation.Expression);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

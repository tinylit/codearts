using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// 核心。
    /// </summary>
    public class CoreVisitor : BaseVisitor
    {
        #region 匿名内部类

        /// <summary>
        /// 智能开关。
        /// </summary>
        private class WhereSwitch
        {
            private bool isFirst = true;

            private readonly Action firstAction;
            private readonly Action unFirstAction;

            public WhereSwitch(Action firstAction, Action unFirstAction)
            {
                this.firstAction = firstAction;
                this.unFirstAction = unFirstAction;
            }

            public void Execute()
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

        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldExpression;
            private readonly Expression _newExpression;

            public ReplaceExpressionVisitor(Expression oldExpression, Expression newExpression)
            {
                _oldExpression = oldExpression;
                _newExpression = newExpression;
            }
            public override Expression Visit(Expression node)
            {
                if (_oldExpression == node)
                {
                    return base.Visit(_newExpression);
                }

                return base.Visit(node);
            }
        }
        #endregion

        /// <summary>
        /// 条件。
        /// </summary>
        private readonly WhereSwitch whereSwitch;

        private readonly bool hasVisitor = false;
        private readonly BaseVisitor visitor;

        /// <inheritdoc />
        public CoreVisitor(ISQLCorrectSettings settings) : base(settings)
        {
            whereSwitch = new WhereSwitch(writer.Where, writer.And);
        }

        /// <summary>
        /// 条件类型。
        /// </summary>
        public enum ConditionType
        {
            /// <summary>
            /// WHERE
            /// </summary>
            Where,
            /// <summary>
            /// AND
            /// </summary>
            And,
            /// <summary>
            /// HAVING
            /// </summary>
            Having
        }

        /// <inheritdoc />
        public CoreVisitor(BaseVisitor visitor, bool isNewWriter = false, ConditionType conditionType = ConditionType.Where) : base(visitor, isNewWriter)
        {
            this.visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));

            hasVisitor = true;

            switch (conditionType)
            {
                case ConditionType.And:
                    whereSwitch = new WhereSwitch(writer.And, writer.And);
                    break;
                case ConditionType.Having:
                    whereSwitch = new WhereSwitch(writer.Having, writer.And);
                    break;
                case ConditionType.Where:
                default:
                    whereSwitch = new WhereSwitch(writer.Where, writer.And);
                    break;
            }
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var declaringType = node.Method.DeclaringType;

            if (declaringType == Types.Queryable)
            {
                VisitCore(node);
            }
            else if (declaringType == Types.RepositoryExtentions)
            {
                VisitOfLts(node);
            }
            else if (declaringType == Types.Enumerable)
            {
                VisitLinq(node);
            }
            else if (declaringType == Types.String)
            {
                VisitOfString(node);
            }
            else if (Types.IEnumerable.IsAssignableFrom(declaringType))
            {
                VisitSet(node);
            }
            else
            {
                base.VisitMethodCall(node);
            }

            return node;
        }

        /// <inheritdoc />
        protected override void VisitCore(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Where:
                case MethodCall.TakeWhile:
                    VisitCondition(node);
                    break;
                case MethodCall.SkipWhile:
                    writer.ReverseCondition(() => VisitCondition(node));
                    break;
                case MethodCall.Any:
                    using (var visitor = new NestedAnyVisitor(this.visitor))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.All:
                    using (var visitor = new NestedAllVisitor(this.visitor))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Contains:
                    using (var visitor = new NestedContainsVisitor(this.visitor))
                    {
                        visitor.Startup(node);
                    }
                    break;
                default:
                    base.VisitCore(node);
                    break;
            }
        }

        private bool IsPlainVariableEx(Expression node)
        {
            switch (node)
            {
                case MethodCallExpression method when method.Method.Name == MethodCall.From && method.Method.DeclaringType == Types.RepositoryExtentions:
                    return IsPlainVariableEx(method.Arguments[0]);
                default:
                    return IsPlainVariable(node);
            }
        }

        #region Core
        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual internal void VisitCondition(MethodCallExpression node)
        {
            bool isPlainVariable = IsPlainVariableEx(node.Arguments[0]);

            if (!isPlainVariable)
            {
                Visit(node.Arguments[0]);
            }

            Workflow(whereIsNotEmpty =>
            {
                if (isPlainVariable)
                {
                    Visit(node.Arguments[0]);
                }

                if (whereIsNotEmpty)
                {
                    whereSwitch.Execute();
                }

            }, () =>
            {
                using (var visitor = new WhereVisitor(this))
                {
                    visitor.Startup(node.Arguments[1]);
                }
            });
        }
        #endregion

        /// <summary>
        /// <see cref="Enumerable"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitLinq(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Any:
                    VisitLinqAny(node);
                    break;
                case MethodCall.All:
                    goto default;
                case MethodCall.Contains:
                    VisitLinqContains(node);
                    break;
                case MethodCall.First:
                case MethodCall.FirstOrDefault:
                case MethodCall.Last:
                case MethodCall.LastOrDefault:
                case MethodCall.Single:
                case MethodCall.SingleOrDefault:
                case MethodCall.ElementAt:
                case MethodCall.ElementAtOrDefault:
                case MethodCall.Count:
                case MethodCall.LongCount:
                case MethodCall.Max:
                case MethodCall.Min:
                case MethodCall.Average:
                    writer.Parameter(node.GetValueFromExpression());
                    break;
                default:
                    VisitByCustom(node);
                    break;
            }
        }

        #region Enumerable
        /// <summary>
        /// <see cref="Enumerable.Any{TSource}(IEnumerable{TSource})"/>、<seealso cref="Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitLinqAny(MethodCallExpression node)
        {
            using (var visitor = new LinqAnyVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        /// <summary>
        /// <see cref="Enumerable.Contains{TSource}(IEnumerable{TSource}, TSource)"/>
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitLinqContains(MethodCallExpression node)
        {
            using (var visitor = new LinqContainsVisitor(this))
            {
                visitor.Startup(node);
            }
        }
        #endregion

        /// <summary>
        /// System.Collections.IEnumerable 的函数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected virtual void VisitSet(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case "Exists":
                    VisitSetExists(node);
                    break;
                case "TrueForAll":
                    goto default;
                case MethodCall.Contains:
                    VisitSetContains(node);
                    break;
                case "get_Item":
                    writer.Parameter(node.GetValueFromExpression());
                    break;
                default:
                    VisitByCustom(node);
                    break;
            }
        }

        /// <summary>
        /// <see cref="List{T}.Exists(Predicate{T})"/>
        /// </summary>
        protected virtual void VisitSetExists(MethodCallExpression node)
        {
            using (var visitor = new SetExistsVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        /// <summary>
        /// <see cref="ICollection{T}.Contains(T)"/>
        /// </summary>
        protected virtual void VisitSetContains(MethodCallExpression node)
        {
            using (var visitor = new SetContainsVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        bool DoneAs(MemberInfo memberInfo)
        {
            Type memberType;

            switch (memberInfo.MemberType)
            {
                case MemberTypes.Constructor when memberInfo is ConstructorInfo constructor:
                    memberType = constructor.DeclaringType;
                    break;
                case MemberTypes.Field when memberInfo is FieldInfo field:
                    memberType = field.FieldType;
                    break;
                case MemberTypes.Property when memberInfo is PropertyInfo property:
                    memberType = property.PropertyType;
                    break;
                case MemberTypes.TypeInfo:
                case MemberTypes.Custom:
                case MemberTypes.NestedType:
                case MemberTypes.All:
                case MemberTypes.Event:
                case MemberTypes.Method:
                default:
                    return false;
            }

            return memberType.IsValueType || memberType == typeof(string) || memberType == typeof(Version);
        }

        private void VisitMyMember(Expression node)
        {
            switch (node)
            {
                case MethodCallExpression method when method.Method.DeclaringType == Types.Queryable:
                    switch (method.Method.Name)
                    {
                        case MethodCall.Any:
                            writer.Write("CASE WHEN ");
                            using (var visitor = new NestedAnyVisitor(this))
                            {
                                visitor.Startup(node);
                            }
                            writer.Write(" THEN ");
                            writer.BooleanTrue();
                            writer.Write(" ELSE ");
                            writer.BooleanFalse();
                            writer.Write(" END");

                            break;
                        case MethodCall.All:
                            writer.Write("CASE WHEN ");
                            using (var visitor = new NestedAllVisitor(this))
                            {
                                visitor.Startup(node);
                            }
                            writer.Write(" THEN ");
                            writer.BooleanTrue();
                            writer.Write(" ELSE ");
                            writer.BooleanFalse();
                            writer.Write(" END");
                            break;
                        default:
                            using (var visitor = new SelectVisitor(this))
                            {
                                writer.OpenBrace();
                                visitor.Startup(node);
                                writer.CloseBrace();
                            }
                            break;
                    }
                    break;
                default:
                    base.VisitTail(node);

                    break;
            }
        }

        /// <summary>
        /// 访问<see cref="NewExpression"/>成员。
        /// </summary>
        /// <param name="memberInfo">成员。</param>
        /// <param name="node">成员表达式。</param>
        /// <returns></returns>
        protected internal virtual void VisitNewMember(MemberInfo memberInfo, Expression node) => VisitMyMember(node);

        /// <inheritdoc />
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            VisitMyMember(node.Expression);

            return node;
        }

        /// <summary>
        /// 定义<see cref="NewExpression"/>成员的别名。
        /// </summary>
        /// <param name="memberInfo">成员。</param>
        /// <param name="memberOfHostType">表达式类型。</param>
        protected virtual void DefNewMemberAs(MemberInfo memberInfo, Type memberOfHostType)
        {
        }

        /// <inheritdoc />
        protected override Expression VisitNew(NewExpression node)
        {
            var members = FilterMembers(node.Members);

            var enumerator = members.GetEnumerator();

            if (enumerator.MoveNext())
            {
                Type memberOfHostType = node.Type;

                Done(enumerator.Current);

                while (enumerator.MoveNext())
                {
                    writer.Delimiter();

                    Done(enumerator.Current);
                }

                return node;

                void Done(MemberInfo memberInfo)
                {
                    VisitNewMember(memberInfo, node.Arguments[node.Members.IndexOf(memberInfo)]);

                    if (DoneAs(memberInfo))
                    {
                        DefNewMemberAs(memberInfo, memberOfHostType);
                    }
                }
            }
            else
            {
                throw new DSyntaxErrorException("未指定查询字段!");
            }
        }

        /// <summary>
        /// 定义<see cref="MemberBinding"/>的别名。
        /// </summary>
        /// <param name="member">成员。</param>
        /// <param name="memberOfHostType">成员所在类型。</param>

        protected virtual void DefMemberBindingAs(MemberBinding member, Type memberOfHostType)
        {
        }

        /// <inheritdoc />
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            var bindings = FilterMemberBindings(node.Bindings);

            var enumerator = bindings.GetEnumerator();

            if (enumerator.MoveNext())
            {
                Done(enumerator.Current);

                while (enumerator.MoveNext())
                {
                    writer.Delimiter();

                    Done(enumerator.Current);
                }

                return node;

                void Done(MemberBinding member)
                {
                    VisitMemberBinding(member);

                    if (DoneAs(member.Member))
                    {
                        DefMemberBindingAs(member, node.Type);
                    }
                }
            }
            else
            {
                throw new DException("未指定查询字段!");
            }
        }

        /// <inheritdoc />
        protected internal override Type TypeToEntryType(Type repositoryType, bool throwsError = true)
        {
            if (hasVisitor && visitor is SelectVisitor)
            {
                return visitor.TypeToEntryType(repositoryType, throwsError);
            }

            return base.TypeToEntryType(repositoryType, throwsError);
        }
    }
}

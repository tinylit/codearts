using System;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{System.Func{TSource, bool}})"/>、<seealso cref="Queryable.TakeWhile{TSource}(IQueryable{TSource}, Expression{System.Func{TSource, bool}})"/>、<seealso cref="Queryable.SkipWhile{TSource}(IQueryable{TSource}, Expression{System.Func{TSource, bool}})"/>的第二个参数。
    /// </summary>
    public class WhereVisitor : BaseVisitor
    {
        private readonly BaseVisitor visitor;

        /// <summary>
        /// 条件的两端。
        /// </summary>
        private bool isConditionBalance = false;

        /// <summary>
        /// 忽略可空类型。
        /// </summary>
        private bool ignoreNullable = false;

        /// <inheritdoc />
        public WhereVisitor(BaseVisitor visitor) : base(visitor, false)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            AnalysisAlias(node.Parameters[0]);

            switch (node.Body)
            {
                case ConstantExpression constant:
                    if (Equals(constant.Value, false) && !writer.IsReverseCondition)
                    {
                        writer.BooleanTrue();

                        writer.Equal();

                        writer.BooleanFalse();
                    }
                    break;
                case MemberExpression member:
                    if (member.IsHasValue())
                    {
                        VisitMember(member);

                        break;
                    }

                    isConditionBalance = true;

                    VisitMember(member);

                    isConditionBalance = false;

                    break;
                default:
                    Visit(node.Body);

                    break;
            }

            return node;
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == Types.Queryable)
            {
                VisitCore(node);

                return node;
            }

            return visitor.Visit(node);
        }

        /// <inheritdoc />
        protected override void VisitCore(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
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
                case MethodCall.Select:
                    using (var visitor = new SelectVisitor(this.visitor))
                    {
                        writer.OpenBrace();

                        visitor.Startup(node);

                        writer.CloseBrace();
                    }
                    break;
                default:
                    if (node.Type.IsValueType || node.Type == Types.String || !node.Type.IsQueryable())
                    {
                        goto case MethodCall.Select;
                    }

                    visitor.Visit(node);

                    break;
            }
        }

        /// <inheritdoc />
        protected override void VisitBinaryIsBooleanLeft(Expression node)
        {
            ignoreNullable = node.Type.IsNullable();

            base.VisitBinaryIsBooleanLeft(node);

            ignoreNullable = false;
        }

        /// <inheritdoc />
        protected override void VisitBinaryIsIsBooleanRight(Expression node)
        {
            ignoreNullable = node.Type.IsNullable();

            base.VisitBinaryIsIsBooleanRight(node);

            ignoreNullable = false;
        }

        /// <inheritdoc />
        protected override void VisitBinaryIsBit(Expression node)
        {
            ignoreNullable = true;

            base.VisitBinaryIsBit(node);

            ignoreNullable = false;
        }

        /// <inheritdoc />
        protected override void VisitBinaryIsConditionToVisit(Expression node)
        {
            isConditionBalance = true;

            base.VisitBinaryIsConditionToVisit(node);

            isConditionBalance = false;
        }

        /// <inheritdoc />
        protected override void VisitMemberIsVariable(MemberExpression node)
        {
            if (ignoreNullable || isConditionBalance)
            {
                var value = node.GetValueFromExpression();

                if (ignoreNullable && value is null)
                {
                    return;
                }

                if (isConditionBalance)
                {
                    if (value.Equals(!writer.IsReverseCondition))
                    {
                        return;
                    }

                    base.VisitMemberIsVariable(node);

                    if (node.IsBoolean())
                    {
                        writer.Equal();

                        writer.BooleanFalse();
                    }

                    return;
                }
            }

            base.VisitMemberIsVariable(node);
        }

        /// <inheritdoc />
        protected override void VisitMemberIsDependOnParameterTypeIsPlain(MemberExpression node)
        {
            base.VisitMemberIsDependOnParameterTypeIsPlain(node);

            if (isConditionBalance && node.IsBoolean())
            {
                writer.Equal();

                writer.BooleanTrue();
            }
        }
    }
}

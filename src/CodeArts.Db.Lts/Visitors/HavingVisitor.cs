using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.GroupBy{TSource, TKey}(IQueryable{TSource}, Expression{Func{TSource, TKey}})"/>.<seealso cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
    /// </summary>
    public class HavingVisitor : WhereVisitor
    {
        private readonly GroupByVisitor visitor;
        private readonly Dictionary<MemberInfo, Expression> groupByExpressions;

        /// <inheritdoc />
        public HavingVisitor(GroupByVisitor visitor, Dictionary<MemberInfo, Expression> groupByExpressions) : base(visitor)
        {
            this.visitor = visitor;
            this.groupByExpressions = groupByExpressions;
        }

        /// <inheritdoc />
        protected internal override void VisitNewMember(MemberInfo memberInfo, Expression node)
        {
            if (groupByExpressions.TryGetValue(memberInfo, out Expression expression))
            {
                visitor.Visit(expression);
            }
            else
            {
                throw new DSyntaxErrorException();
            }

            writer.Equal();

            base.VisitNewMember(memberInfo, node);
        }

        /// <inheritdoc />
        protected override void MemberDelimiter() => writer.And();

        /// <inheritdoc />
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            if (groupByExpressions.TryGetValue(node.Member, out Expression expression))
            {
                visitor.Visit(expression);
            }
            else
            {
                throw new DSyntaxErrorException();
            }

            writer.Equal();

            return base.VisitMemberAssignment(node);
        }

        /// <inheritdoc />
        protected override void VisitBinaryIsBoolean(Expression left, ExpressionType expressionType, Expression right)
        {
            if (expressionType != ExpressionType.Equal && expressionType != ExpressionType.NotEqual)
            {
                base.VisitBinaryIsBoolean(left, expressionType, right);
            }
            else if (left.IsGrouping())
            {
                if (expressionType == ExpressionType.NotEqual)
                {
                    writer.ReverseCondition(() => VisitTail(right));
                }
                else
                {
                    VisitTail(right);
                }
            }
            else if (right.IsGrouping())
            {
                if (expressionType == ExpressionType.NotEqual)
                {
                    writer.ReverseCondition(() => VisitTail(left));
                }
                else
                {
                    VisitTail(left);
                }
            }
            else
            {
                base.VisitBinaryIsBoolean(left, expressionType, right);
            }
        }
    }
}

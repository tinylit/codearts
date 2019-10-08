using SkyBuilding.ORM.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 解决表达式
    /// </summary>
    public static class ExpressionResolve
    {
        private class Visiter : ExpressionVisitor
        {
            private readonly List<string> columns;
            public Visiter()
            {
                columns = new List<string>();
            }

            public void Evaluate(NewExpression node) => base.Visit(node);

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                base.Visit(node.Body);

                return node;
            }

            protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
            {
                columns.Add(node.Member.Name);

                return node;
            }
        }

        public static List<Func<T, string>> Coalesce<T>(ParameterExpression parameter, BinaryExpression binary)
        {
            var bodyExp = Recursive(binary);

            var lamdaExp = Expression.Lambda<Func<T, string>>(bodyExp, parameter);

            return new List<Func<T, string>> { lamdaExp.Compile() };
        }

        public static List<Func<T, string>> Conditional<T>(ParameterExpression parameter, ConditionalExpression conditional)
        {
            var bodyExp = Recursive(null);

            var lamdaExp = Expression.Lambda<Func<T, string>>(bodyExp, parameter);

            return new List<Func<T, string>> { lamdaExp.Compile() };
        }

        private static Expression Recursive(BinaryExpression binary)
        {
            if (!(binary.Left is MemberExpression left))
                throw new ExpressionNotSupportedException();

            var test = Expression.NotEqual(left, Expression.Default(binary.Type));

            var ifTrue = Expression.Constant(left.Member.Name);

            if (binary.Right is MemberExpression right)
                return Expression.IfThenElse(test, ifTrue, Expression.Constant(right.Member.Name));

            if (binary.Right is BinaryExpression binary2)
                return Expression.IfThenElse(test, ifTrue, Recursive(binary2));

            throw new ExpressionNotSupportedException();
        }
    }
}

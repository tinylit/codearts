using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Expressions
{
    /// <summary>
    /// <see cref="Enumerable.Any{TSource}(IEnumerable{TSource})"/>和<seealso cref="Enumerable.Any{TSource}(IEnumerable{TSource}, Func{TSource, bool})"/>
    /// </summary>
    public class LinqAnyVisitor : WhereVisitor
    {
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

        private object valueCurrent;

        /// <inheritdoc />
        public LinqAnyVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.Any && node.Method.DeclaringType == Types.Enumerable;

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            var value = node.Arguments[0].GetValueFromExpression();

            IEnumerable valueSet = value as IEnumerable ?? Enumerable.Empty<object>();

            if (node.Arguments.Count == 1)
            {
                VisitAny(valueSet);
            }
            else
            {
                VisitAny(valueSet, node.Arguments[1]);
            }
        }

        /// <summary>
        /// 单个参数。
        /// </summary>
        /// <param name="ts">迭代器。</param>
        protected virtual void VisitAny(IEnumerable ts)
        {
            var enumerator = ts.GetEnumerator();

            if (!(enumerator.MoveNext() ^ writer.IsReverseCondition))
            {
                writer.BooleanTrue();

                writer.Equal();

                writer.BooleanFalse();
            }
        }

        /// <summary>
        /// 单个参数。
        /// </summary>
        /// <param name="ts">迭代器。</param>
        /// <param name="node">参数。</param>
        protected virtual void VisitAny(IEnumerable ts, Expression node)
        {
            var enumerator = ts.GetEnumerator();

            if (enumerator.MoveNext())
            {
                writer.OpenBrace();

                valueCurrent = enumerator.Current;

                Visit(node);

                while (enumerator.MoveNext())
                {
                    writer.Or();

                    valueCurrent = enumerator.Current;

                    Visit(node);
                }

                writer.CloseBrace();
            }
            else if (!writer.IsReverseCondition)
            {
                writer.BooleanTrue();

                writer.Equal();

                writer.BooleanFalse();
            }
        }

        /// <inheritdoc />
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var visitor = new ReplaceExpressionVisitor(node.Parameters[0], Expression.Constant(valueCurrent));

            Visit(visitor.Visit(node.Body));

            return node;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="List{T}.Exists(Predicate{T})"/>
    /// </summary>
    public class SetExistsVisitor : BaseVisitor
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
        public SetExistsVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == "Exists" && Types.IEnumerable.IsAssignableFrom(node.Method.DeclaringType);

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            var value = node.Object.GetValueFromExpression();

            IEnumerable valueSet = value as IEnumerable ?? Enumerable.Empty<object>();

            VisitExists(valueSet, node.Arguments[0]);
        }

        /// <summary>
        /// 单个参数。
        /// </summary>
        /// <param name="ts">迭代器。</param>
        /// <param name="node">参数。</param>
        protected virtual void VisitExists(IEnumerable ts, Expression node)
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

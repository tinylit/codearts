using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="ICollection{T}.Contains(T)"/>
    /// </summary>
    public class SetContainsVisitor : BaseVisitor
    {
        private readonly BaseVisitor visitor;

        /// <inheritdoc />
        public SetContainsVisitor(BaseVisitor visitor) : base(visitor, false)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.Contains && Types.IEnumerable.IsAssignableFrom(node.Method.DeclaringType);

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            var value = node.Object.GetValueFromExpression();

            if (value is null)
            {
                return;
            }

            Expression expression = node.Arguments[0];

            Workflow(whereIsNotEmpty =>
            {
                if (whereIsNotEmpty)
                {
                    visitor.Visit(expression);
                }
                else if (!writer.IsReverseCondition)
                {
                    writer.BooleanTrue();

                    writer.Equal();

                    writer.BooleanFalse();
                }

            }, () => VisitContains(value as IEnumerable, expression));
        }

        /// <summary>
        /// 单个参数。
        /// </summary>
        /// <param name="ts">迭代器。</param>
        /// <param name="node">参数。</param>
        protected virtual void VisitContains(IEnumerable ts, Expression node)
        {
            var enumerator = ts.GetEnumerator();

            if (enumerator.MoveNext())
            {
                int parameterCount = 0;

                writer.Contains();
                writer.OpenBrace();
                writer.Parameter(enumerator.Current);

                int maxParamterCount;

                switch (settings.Engine)
                {
                    case DatabaseEngine.Normal:
                    case DatabaseEngine.SQLite:
                        maxParamterCount = 1000;
                        break;
                    case DatabaseEngine.SqlServer:
                        maxParamterCount = 10000;
                        break;
                    case DatabaseEngine.MySQL:
                        maxParamterCount = 20000;
                        break;
                    case DatabaseEngine.Oracle:
                        maxParamterCount = 256;
                        break;
                    case DatabaseEngine.PostgreSQL:
                    case DatabaseEngine.DB2:
                    case DatabaseEngine.Sybase:
                    case DatabaseEngine.Access:
                    default:
                        maxParamterCount = 128;
                        break;
                }

                while (enumerator.MoveNext())
                {
                    if (parameterCount < maxParamterCount)
                    {
                        writer.Delimiter();
                    }
                    else
                    {
                        parameterCount = 0;

                        writer.CloseBrace();
                        writer.WhiteSpace();
                        writer.Write("OR");
                        writer.WhiteSpace();

                        visitor.Visit(node);

                        writer.Contains();
                        writer.OpenBrace();
                    }

                    writer.Parameter(enumerator.Current);
                }

                writer.CloseBrace();
            }
        }
    }
}

using System;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.Any{TSource}(IQueryable{TSource})"/>、<seealso cref="Queryable.Any{TSource}(IQueryable{TSource}, System.Linq.Expressions.Expression{Func{TSource, bool}})"/>.
    /// </summary>
    public class AnyVisitor : BaseVisitor
    {
        private readonly SelectVisitor visitor;

        /// <inheritdoc />
        public AnyVisitor(SelectVisitor visitor) : base(visitor)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.Any && node.Method.DeclaringType == Types.Queryable;

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            writer.Select();

            writer.Write("CASE WHEN ");
            writer.Exists();
            writer.OpenBrace();

            if (writer.IsReverseCondition)
            {
                writer.ReverseCondition(Done);
            }
            else
            {
                Done();
            }

            void Done()
            {
                if (node.Arguments.Count == 1)
                {
                    visitor.Visit(node.Arguments[0]);
                }
                else
                {
                    visitor.VisitCondition(node);
                }
            }

            writer.CloseBrace();
            writer.Write(" THEN ");

            writer.BooleanTrue();

            writer.Write(" ELSE ");

            writer.BooleanFalse();

            writer.Write(" END");
        }
    }
}

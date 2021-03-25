using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.Contains{TSource}(IQueryable{TSource}, TSource)"/>.
    /// </summary>
    public class NestedContainsVisitor : BaseVisitor
    {
        /// <inheritdoc />
        public NestedContainsVisitor(BaseVisitor visitor) : base(visitor, false)
        {
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.DeclaringType == Types.Queryable && node.Method.Name == MethodCall.Contains && node.Arguments.Count == 2 && !IsPlainVariable(node.Arguments[1]);

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            base.Visit(node.Arguments[1]);

            writer.Contains();

            writer.OpenBrace();

            using (var visitor = new SelectVisitor(this))
            {
                visitor.Startup(node.Arguments[0]);
            }

            writer.CloseBrace();
        }
    }
}

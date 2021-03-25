using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// WHERE ALL
    /// </summary>
    public class NestedAllVisitor : SelectVisitor
    {
        /// <inheritdoc />
        public NestedAllVisitor(BaseVisitor visitor) : base(visitor, false)
        {

        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.DeclaringType == Types.Queryable && node.Method.Name == MethodCall.All;

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            writer.ReverseCondition(() =>
            {
                writer.Exists();
                writer.OpenBrace();

                if (node.Arguments.Count == 1)
                {
                    base.Visit(node.Arguments[0]);
                }
                else
                {
                    base.VisitCondition(node);
                }

                writer.CloseBrace();
            });
        }
    }
}

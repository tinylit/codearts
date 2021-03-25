using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// All函数。
    /// </summary>
    public class AllVisitor : BaseVisitor
    {
        private readonly SelectVisitor visitor;

        /// <inheritdoc />
        public AllVisitor(SelectVisitor visitor) : base(visitor, false)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.All && node.Method.DeclaringType == Types.Queryable;

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            writer.Select();
            writer.Write("CASE WHEN ");

            writer.ReverseCondition(() =>
            {
                writer.Exists();
                writer.OpenBrace();

                if (node.Arguments.Count == 1)
                {
                    visitor.Visit(node.Arguments[0]);
                }
                else
                {
                    visitor.VisitCondition(node);
                }

                writer.CloseBrace();
            });

            writer.Write(" THEN ");

            writer.BooleanTrue();

            writer.Write(" ELSE ");

            writer.BooleanFalse();

            writer.Write(" END");
        }
    }
}

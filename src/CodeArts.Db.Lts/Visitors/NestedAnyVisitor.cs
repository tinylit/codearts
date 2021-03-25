using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// WHERE ANY
    /// </summary>
    public class NestedAnyVisitor : SelectVisitor
    {
        /// <inheritdoc />
        public NestedAnyVisitor(BaseVisitor visitor) : base(visitor, false)
        {

        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.DeclaringType == Types.Queryable && node.Method.Name == MethodCall.Any;

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
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
                    Visit(node.Arguments[0]);
                }
                else
                {
                    VisitCondition(node);
                }
            }

            writer.CloseBrace();
        }
    }
}

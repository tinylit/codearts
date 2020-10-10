using System;
using System.Linq.Expressions;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// Any函数。
    /// </summary>
    public class AnyVisitor : BaseVisitor
    {
        private class AnySelectVisitor : SelectVisitor
        {
            public AnySelectVisitor(BaseVisitor visitor) : base(visitor)
            {
            }

            /// <inheritdoc />
            public override bool CanResolve(MethodCallExpression node) =>
                node.Method.Name == MethodCall.Any || node.Method.Name == MethodCall.All;

            /// <inheritdoc />
            protected override Expression StartupCore(MethodCallExpression node)
            {
                if (node.Arguments.Count == 1)
                {
                    return base.Visit(node.Arguments[0]);
                }
                else
                {
                    return base.VisitCondition(node);
                }
            }
        }

        /// <inheritdoc />
        public AnyVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) =>
            node.Method.Name == MethodCall.Any || node.Method.Name == MethodCall.All;


        /// <inheritdoc />
        protected override Expression StartupCore(MethodCallExpression node)
        {
            writer.Exists();

            writer.OpenBrace();

            if (node.Arguments.Count == 1)
            {
                using (var visitor = new SelectVisitor(this))
                {
                    visitor.Startup(node.Arguments[0]);
                }
            }
            else
            {
                using (var visitor = new AnySelectVisitor(this))
                {
                    visitor.Startup(node);
                }
            }

            writer.CloseBrace();

            return node;
        }
    }
}

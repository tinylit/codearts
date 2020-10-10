using System;
using System.Linq.Expressions;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// All函数。
    /// </summary>
    public class AllVisitor : BaseVisitor
    {
        /// <inheritdoc />
        public AllVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.All;

        /// <inheritdoc />
        protected override Expression StartupCore(MethodCallExpression node)
        {
            writer.OpenBrace();

            using (var visitor = new AnyVisitor(this))
            {
                visitor.Startup(node);
            }

            writer.And();

            InvertWhere(() =>
            {
                using (var visitor = new AnyVisitor(this))
                {
                    visitor.Startup(node);
                }
            });

            writer.CloseBrace();

            return node;
        }
    }
}

using System;
using System.Linq.Expressions;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// All函数。
    /// </summary>
    public class AllVisitor : BaseVisitor
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public AllVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <summary>
        /// inherit。
        /// </summary>
        /// <returns></returns>
        protected override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.All;

        /// <summary>
        /// inherit。
        /// </summary>
        /// <returns></returns>
        protected override Expression StartupCore(MethodCallExpression node)
        {
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

            return node;
        }
    }
}

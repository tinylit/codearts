using System.Linq.Expressions;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// Where。
    /// </summary>
    public class WhereVisitor : BaseVisitor
    {
        /// <summary>
        /// inherit。
        /// </summary>
        public WhereVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <summary>
        /// inherit。
        /// </summary>
        /// <returns></returns>
        protected override Expression VisitOfQueryableAny(MethodCallExpression node)
        {
            try
            {
                writer.OpenBrace();

                return base.VisitOfQueryableAny(node);
            }
            finally
            {
                writer.CloseBrace();
            }
        }

        /// <summary>
        /// inherit。
        /// </summary>
        /// <returns></returns>
        protected override Expression VisitOfQueryableAll(MethodCallExpression node)
        {
            try
            {
                writer.OpenBrace();

                return base.VisitOfQueryableAll(node);
            }
            finally
            {
                writer.CloseBrace();
            }
        }
    }
}

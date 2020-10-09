using System.Linq.Expressions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 访问器。
    /// </summary>
    public interface IVisitor
    {
        /// <summary>
        /// 能否解决表达式。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        bool CanResolve(MethodCallExpression node);
    }
}

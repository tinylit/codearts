using System.Linq.Expressions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 访问器
    /// </summary>
    public interface IVisitor
    {
        /// <summary>
        /// 能否解决表达式
        /// </summary>
        /// <param name="node">表达式</param>
        /// <returns></returns>
        bool CanResolve(MethodCallExpression node);

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="visitor">访问器</param>
        /// <param name="writer">写入SQL查询中</param>
        /// <param name="node">表达式</param>
        Expression Visit(ExpressionVisitor visitor, Writer writer, MethodCallExpression node);
    }
}

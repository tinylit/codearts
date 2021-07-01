using System.Linq.Expressions;

namespace CodeArts.Db
{
    /// <summary>
    /// 自定义的访问器。
    /// </summary>
    public interface ICustomVisitor : IVisitor
    {
        /// <summary>
        /// 表达式分析。
        /// </summary>
        /// <param name="visitor">访问器。</param>
        /// <param name="writer">SQL写入器。</param>
        /// <param name="node">表达式。</param>
        void Visit(ExpressionVisitor visitor, Writer writer, MethodCallExpression node);
    }
}

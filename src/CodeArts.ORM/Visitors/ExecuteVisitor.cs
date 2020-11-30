using System;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// 执行力访问器。
    /// </summary>
    public class ExecuteVisitor : BaseVisitor, IExecuteVisitor
    {
        /// <inheritdoc />
        public ExecuteVisitor(ISQLCorrectSettings settings) : base(settings)
        {
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node)
            => (node.Method.Name == MethodCall.Insert || node.Method.Name == MethodCall.Update || node.Method.Name == MethodCall.Delete) && node.Method.DeclaringType == typeof(RepositoryExtentions);

        private Expression Visit(IExecuteVisitor visitor, Expression node)
        {
            try
            {
                return visitor.Startup(node);
            }
            finally
            {
                Behavior = visitor.Behavior;
                TimeOut = visitor.TimeOut;
            }
        }

        /// <inheritdoc />
        protected override Expression VisitOfSelect(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Insert:
                    using (var visitor = new InsertVisitor(this))
                    {
                        return Visit(visitor, node);
                    }
                case MethodCall.Update:
                    using (var visitor = new UpdateVisitor(this))
                    {
                        return Visit(visitor, node);
                    }
                case MethodCall.Delete:
                    using (var visitor = new DeleteVisitor(this))
                    {
                        return Visit(visitor, node);
                    }
                default:
                    throw new NotSupportedException($"类型“{node.Method.DeclaringType}”的函数“{node.Method.Name}”不被支持!");
            }
        }

        /// <summary>
        /// 指令行为。
        /// </summary>
        public ActionBehavior Behavior { private set; get; }

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。
        /// </summary>
        public int? TimeOut { private set; get; }
    }
}

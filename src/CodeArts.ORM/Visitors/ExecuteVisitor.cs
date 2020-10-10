using System;
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
            => (node.Method.Name == MethodCall.Insert || node.Method.Name == MethodCall.Update || node.Method.Name == MethodCall.Delete) && node.Method.DeclaringType == typeof(Executeable);

        internal Expression VisitOfTimeOut(MethodCallExpression node)
        {
            TimeOut += (int)node.Arguments[1].GetValueFromExpression();

            return node.Arguments[0];
        }

        /// <inheritdoc />
        protected override Expression VisitOfExecuteable(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Insert:
                    Behavior = CommandBehavior.Insert;
                    using (var visitor = new InsertVisitor(this))
                    {
                        return visitor.Startup(node);
                    }
                case MethodCall.Update:
                    Behavior = CommandBehavior.Update;
                    using (var visitor = new UpdateVisitor(this))
                    {
                        return visitor.Startup(node);
                    }
                case MethodCall.Delete:
                    Behavior = CommandBehavior.Delete;
                    using (var visitor = new DeleteVisitor(this))
                    {
                        return visitor.Startup(node);
                    }
                case MethodCall.TimeOut:
                    return base.Visit(VisitOfTimeOut(node));
                default:
                    throw new NotSupportedException($"类型“{node.Method.DeclaringType}”的函数“{node.Method.Name}”不被支持!");
            }
        }

        /// <summary>
        /// 指令行为。
        /// </summary>
        public CommandBehavior Behavior { private set; get; }

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。
        /// </summary>
        public int? TimeOut { private set; get; }
    }
}

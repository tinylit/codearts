using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace CodeArts.Db.Expressions
{
    /// <summary>
    /// 执行力访问器。
    /// </summary>
    public class ExecuteVisitor : BaseVisitor, IExecuteVisitor
    {
        private readonly ICustomVisitorList visitors;

        /// <inheritdoc />
        public ExecuteVisitor(ISQLCorrectSettings settings, ICustomVisitorList visitors) : base(settings)
        {
            this.visitors = visitors;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node)
            => (node.Method.Name == MethodCall.Insert || node.Method.Name == MethodCall.Update || node.Method.Name == MethodCall.Delete) && node.Method.DeclaringType == Types.RepositoryExtentions;

        /// <summary>
        /// 设置超时时间。
        /// </summary>
        public void SetTimeOut(int timeOut)
        {
            if (TimeOut.HasValue)
            {
                timeOut += TimeOut.Value;
            }

            TimeOut = new int?(timeOut);
        }

        /// <inheritdoc />
        protected override IEnumerable<ICustomVisitor> GetCustomVisitors() => visitors;

        /// <inheritdoc />
        protected override void VisitOfLts(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Insert:
                    Behavior = ActionBehavior.Insert;

                    using (var visitor = new InsertVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Update:
                    Behavior = ActionBehavior.Update;

                    using (var visitor = new UpdateVisitor(this))
                    {
                       visitor.Startup(node);
                    }
                    break;
                case MethodCall.Delete:
                    Behavior = ActionBehavior.Delete;
                    using (var visitor = new DeleteVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
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

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
        private Action<CommandSql> watchSql;
        private int? timeOut;
        private readonly ICustomVisitorList visitors;

        /// <inheritdoc />
        public ExecuteVisitor(ISQLCorrectSettings settings, ICustomVisitorList visitors) : base(settings)
        {
            this.visitors = visitors;
        }

        /// <summary>
        /// 创建插入访问器。
        /// </summary>
        /// <returns></returns>
        protected virtual InsertVisitor CreateInsertVisitor() => new InsertVisitor(this);

        /// <summary>
        /// 创建更新访问器。
        /// </summary>
        /// <returns></returns>
        protected virtual UpdateVisitor CreateUpdateVisitor() => new UpdateVisitor(this);

        /// <summary>
        /// 创建删除访问器。
        /// </summary>
        /// <returns></returns>
        protected virtual DeleteVisitor CreateDeleteVisitor() => new DeleteVisitor(this);

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node)
            => (node.Method.Name == MethodCall.Insert || node.Method.Name == MethodCall.Update || node.Method.Name == MethodCall.Delete) && node.Method.DeclaringType == Types.RepositoryExtentions;

        /// <summary>
        /// 设置超时时间。
        /// </summary>
        public void SetTimeOut(int timeOut)
        {
            if (this.timeOut.HasValue)
            {
                this.timeOut += timeOut;
            }
            else
            {
                this.timeOut = new int?(timeOut);
            }
        }

        /// <summary>
        /// SQL监视。
        /// </summary>
        /// <param name="watchSql">SQL监视。</param>
        public void WatchSql(Action<CommandSql> watchSql) => this.watchSql = watchSql;

        /// <inheritdoc />
        protected override IEnumerable<ICustomVisitor> GetCustomVisitors() => visitors;

        /// <inheritdoc />
        protected override void VisitOfLts(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Insert:
                    using (var visitor = CreateInsertVisitor())
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Update:
                    using (var visitor = CreateUpdateVisitor())
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Delete:
                    using (var visitor = CreateDeleteVisitor())
                    {
                        visitor.Startup(node);
                    }
                    break;
                default:
                    throw new NotSupportedException($"类型“{node.Method.DeclaringType}”的函数“{node.Method.Name}”不被支持!");
            }
        }

        /// <summary>
        /// 转SQL。
        /// </summary>
        /// <returns></returns>
        public CommandSql ToSQL()
        {
            var sql = new CommandSql(writer.ToSQL(), writer.Parameters, timeOut);

            watchSql?.Invoke(sql);

            return sql;
        }
    }
}

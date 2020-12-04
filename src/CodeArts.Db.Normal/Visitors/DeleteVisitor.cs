using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Visitors
{
    /// <summary>
    /// 删除访问器。
    /// </summary>
    public class DeleteVisitor : ConditionVisitor, IExecuteVisitor
    {
        /// <inheritdoc />
        public DeleteVisitor(ExecuteVisitor visitor) : base(visitor)
        {
        }

        /// <summary>
        /// 行为。
        /// </summary>
        public ActionBehavior Behavior => ActionBehavior.Delete;

        /// <summary>
        /// 超时时间。
        /// </summary>
        public int? TimeOut { private set; get; }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.Delete && node.Method.DeclaringType == typeof(RepositoryExtentions);

        /// <inheritdoc />
        protected override Expression VisitOfSelect(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.TimeOut:

                    TimeOut += (int)node.Arguments[1].GetValueFromExpression();
                    
                    base.Visit(node.Arguments[0]);

                    return node;
                case MethodCall.Delete:

                    Expression objectExp = node.Arguments[0];

                    Workflow(() =>
                    {
                        writer.Delete();

                        var tableInfo = MakeTableInfo(objectExp.Type);

                        var prefix = GetEntryAlias(tableInfo.TableType, string.Empty);

                        writer.Alias(prefix);

                        writer.From();

                        writer.NameWhiteSpace(GetTableName(tableInfo), prefix);

                    }, () =>
                    {
                        if (node.Arguments.Count > 1)
                        {
                            base.VisitCondition(node);
                        }
                        else
                        {
                            base.Visit(objectExp);
                        }
                    });

                    return node;
                default:
                    return base.VisitOfSelect(node);
            }
        }
    }
}

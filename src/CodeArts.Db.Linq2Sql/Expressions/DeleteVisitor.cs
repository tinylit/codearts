using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Expressions
{
    /// <summary>
    /// 删除访问器。
    /// </summary>
    public class DeleteVisitor : CoreVisitor
    {
        private readonly ExecuteVisitor visitor;

        /// <inheritdoc />
        public DeleteVisitor(ExecuteVisitor visitor) : base(visitor)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.Delete && node.Method.DeclaringType == Types.RepositoryExtentions;

        /// <inheritdoc />
        protected override void VisitOfLts(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.TimeOut:

                    visitor.SetTimeOut((int)node.Arguments[1].GetValueFromExpression());

                    base.Visit(node.Arguments[0]);

                    break;

                case MethodCall.Delete:

                    Expression objectExp = node.Arguments[0];

                    Workflow(() =>
                    {
                        writer.Delete();

                        var tableInfo = MakeTableInfo(objectExp.Type);

                        var prefix = GetEntryAlias(tableInfo.TableType, string.Empty);

                        writer.Alias(prefix);

                        writer.From();

                        WriteTableName(tableInfo, prefix);

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

                    break;
                default:
                    base.VisitOfLts(node);

                    break;
            }
        }
    }
}

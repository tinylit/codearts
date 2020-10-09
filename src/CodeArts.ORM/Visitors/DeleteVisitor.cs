using System.Linq.Expressions;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// 删除访问器。
    /// </summary>
    public class DeleteVisitor : ConditionVisitor
    {
        private readonly ExecuteVisitor visitor;

        /// <summary>
        /// inherit。
        /// </summary>
        public DeleteVisitor(ExecuteVisitor visitor) : base(visitor)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.IsExecuteable())
            {
                return node;
            }

            return base.VisitConstant(node);
        }

        /// <summary>
        /// inherit。
        /// </summary>
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.Delete && node.Method.DeclaringType == typeof(Executeable);

        /// <summary>
        /// inherit。
        /// </summary>
        protected override Expression VisitOfExecuteable(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Where:
                    return base.VisitCondition(node);
                case MethodCall.TimeOut:
                    return base.Visit(visitor.VisitOfTimeOut(node));
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
                    return base.VisitOfExecuteable(node);
            }
        }
    }
}

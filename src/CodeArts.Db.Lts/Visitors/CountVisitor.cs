using System;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.Count{TSource}(IQueryable{TSource})"/>或<seealso cref="Queryable.Count{TSource}(IQueryable{TSource}, System.Linq.Expressions.Expression{Func{TSource, bool}})"/>
    /// </summary>
    public class CountVisitor : BaseVisitor
    {
        const string FnName = "COUNT";

        private readonly SelectVisitor visitor;

        /// <inheritdoc />
        public CountVisitor(SelectVisitor visitor) : base(visitor, false)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.DeclaringType == Types.Queryable && (node.Method.Name == MethodCall.Count || node.Method.Name == MethodCall.LongCount);

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            writer.Select();
            writer.Write(FnName);
            writer.OpenBrace();

            Workflow(() =>
            {
                var mainExp = node.Arguments[0];

                var tableInfo = MakeTableInfo(mainExp.Type);

                var alias = GetEntryAlias(tableInfo.TableType, string.Empty);

                if (tableInfo.Keys.Count == 1)
                {
                    string key = tableInfo.Keys.First();

                    foreach (var kv in tableInfo.ReadOrWrites)
                    {
                        if (kv.Key == key)
                        {
                            writer.NameDot(alias, kv.Value);

                            break;
                        }
                    }
                }
                else
                {
                    writer.Write("1");
                }

                writer.CloseBrace();

            }, () =>
            {
                if (node.Arguments.Count == 1)
                {
                    visitor.Visit(node.Arguments[0]);
                }
                else
                {
                    visitor.VisitCondition(node);
                }
            });
        }
    }
}

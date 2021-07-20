using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Expressions
{
    /// <summary>
    /// WHERE ANY
    /// </summary>
    public class NestedAnyVisitor : SelectVisitor
    {
        /// <inheritdoc />
        public NestedAnyVisitor(BaseVisitor visitor) : base(visitor, false)
        {

        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.DeclaringType == Types.Queryable && node.Method.Name == MethodCall.Any;

        /// <inheritdoc />
        protected override Expression Select(ConstantExpression node)
        {
            var parameterType = TypeToUltimateType(node.Type);

            var prefix = GetEntryAlias(parameterType, string.Empty);

            var tableInfo = MakeTableInfo(parameterType);

            writer.Select();

            var members = FilterMembers(tableInfo.ReadOrWrites);

            var existsMembers = new List<KeyValuePair<string, string>>();

            if (tableInfo.Keys.Count > 0)
            {
                foreach (var item in members)
                {
                    if (tableInfo.Keys.Contains(item.Key))
                    {
                        existsMembers.Add(item);
                    }
                }
            }

            if (existsMembers.Count == tableInfo.Keys.Count)
            {
                WriteMembers(prefix, existsMembers);
            }
            else
            {
                WriteMembers(prefix, members);
            }

            writer.From();

            WriteTableName(tableInfo, prefix);

            return node;
        }

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            writer.Exists();
            writer.OpenBrace();

            if (writer.IsReverseCondition)
            {
                writer.ReverseCondition(Done);
            }
            else
            {
                Done();
            }

            void Done()
            {
                if (node.Arguments.Count == 1)
                {
                    Visit(node.Arguments[0]);
                }
                else
                {
                    VisitCondition(node);
                }
            }

            writer.CloseBrace();
        }
    }
}

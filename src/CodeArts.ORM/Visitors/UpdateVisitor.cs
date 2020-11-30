using CodeArts.ORM.Exceptions;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// 更新访问器。
    /// </summary>
    public class UpdateVisitor : ConditionVisitor, IExecuteVisitor
    {
        /// <summary>
        /// 行为。
        /// </summary>
        public ActionBehavior Behavior => ActionBehavior.Update;

        /// <summary>
        /// 超时时间。
        /// </summary>
        public int? TimeOut { private set; get; }

        /// <inheritdoc />
        public UpdateVisitor(ExecuteVisitor visitor) : base(visitor)
        {
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.Update && node.Method.DeclaringType == typeof(RepositoryExtentions);

        /// <inheritdoc />
        protected override Expression VisitOfSelect(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.TimeOut:

                    TimeOut += (int)node.Arguments[1].GetValueFromExpression();

                    base.Visit(node.Arguments[0]);

                    return node;
                case MethodCall.Update:

                    Expression objectExp = node.Arguments[0];

                    Workflow(() =>
                    {
                        writer.Update();

                        var tableInfo = MakeTableInfo(objectExp.Type);

                        var prefix = GetEntryAlias(tableInfo.TableType, string.Empty);

                        if (settings.Engine == DatabaseEngine.SqlServer || settings.Engine == DatabaseEngine.Access)
                        {
                            writer.Alias(prefix);
                        }
                        else
                        {
                            writer.NameWhiteSpace(GetTableName(tableInfo), prefix);
                        }

                        writer.Set();

                        base.Visit(node.Arguments[1]);

                        if (settings.Engine == DatabaseEngine.SqlServer || settings.Engine == DatabaseEngine.Access)
                        {
                            writer.From();

                            writer.NameWhiteSpace(GetTableName(tableInfo), prefix);
                        }

                    }, () =>
                    {
                        base.Visit(objectExp);
                    });

                    return node;
                default:
                    return base.VisitOfSelect(node);
            }
        }

        /// <inheritdoc />
        protected override Expression VisitNew(NewExpression node)
        {
            var enumerator = node.Members.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var tableInfo = base.MakeTableInfo(node.Type);

                VisitMyMember(enumerator.Current);

                while (enumerator.MoveNext())
                {
                    writer.Delimiter();

                    VisitMyMember(enumerator.Current);
                }

                void VisitMyMember(MemberInfo memberInfo)
                {
                    if (tableInfo.ReadWrites.TryGetValue(memberInfo.Name, out string value))
                    {
                        writer.Write(value);

                        writer.Write("=");

                        VisitCheckIfSubconnection(node.Arguments[node.Members.IndexOf(memberInfo)]);
                    }
                    else
                    {
                        throw new DSyntaxErrorException($"字段“{memberInfo.Name}”不可写!");
                    }
                }

                foreach (var kv in tableInfo.Tokens)
                {
                    if (node.Members.Any(x => x.Name == kv.Key))
                    {
                        continue;
                    }

                    writer.Delimiter();

                    writer.Name(tableInfo.ReadOrWrites[kv.Key]);

                    writer.Write("=");

                    writer.Parameter(kv.Value.Create());
                }

                return node;
            }
            else
            {
                throw new DException("未指定更新字段!");
            }
        }

        /// <inheritdoc />
        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            var bindings = FilterMemberBindings(node.Bindings);

            var enumerator = bindings.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var tableInfo = MakeTableInfo(node.Type);

                VisitMyMemberBinding(enumerator.Current);

                while (enumerator.MoveNext())
                {
                    writer.Delimiter();

                    VisitMyMemberBinding(enumerator.Current);
                }

                void VisitMyMemberBinding(MemberBinding binding)
                {
                    if (tableInfo.ReadWrites.TryGetValue(binding.Member.Name, out string value))
                    {
                        writer.Name(value);

                        writer.Write("=");

                        base.VisitMemberBinding(binding);
                    }
                    else
                    {
                        throw new DSyntaxErrorException($"字段“{binding.Member.Name}”不可写!");
                    }
                }

                foreach (var kv in tableInfo.Tokens)
                {
                    if (node.Bindings.Any(x => x.Member.Name == kv.Key))
                    {
                        continue;
                    }

                    writer.Delimiter();

                    writer.Name(tableInfo.ReadOrWrites[kv.Key]);

                    writer.Write("=");

                    writer.Parameter(kv.Value.Create());
                }

                return node;
            }
            else
            {
                throw new DException("未指定更新字段!");
            }
        }
    }
}

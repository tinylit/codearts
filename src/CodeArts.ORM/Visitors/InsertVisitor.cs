using CodeArts.ORM.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// 插入访问器。
    /// </summary>
    public class InsertVisitor : BaseVisitor, IExecuteVisitor
    {
        private class InsertSelectVisitor : SelectVisitor
        {
            private readonly List<string> insertFields;

            public InsertSelectVisitor(BaseVisitor visitor, List<string> insertFields) : base(visitor)
            {
                this.insertFields = insertFields;
            }

            protected override void WriteMember(string prefix, string field, string alias)
            {
                insertFields.Add(field);

                base.WriteMember(prefix, field, alias);
            }

            protected override void WriteMember(string aggregationName, string prefix, string field, string alias)
            {
                insertFields.Add(field);

                base.WriteMember(aggregationName, prefix, field, alias);
            }

            /// <inheritdoc />
            protected override Expression VisitNew(NewExpression node)
            {
                var members = FilterMembers(node.Members);

                var enumerator = members.GetEnumerator();

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
                            insertFields.Add(value);

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

                        writer.Parameter(kv.Value.Create());

                        insertFields.Add(tableInfo.ReadOrWrites[kv.Key]);
                    }

                    return node;
                }
                else
                {
                    throw new DException("未指定插入字段!");
                }
            }

            /// <inheritdoc />
            protected override Expression VisitMemberInit(MemberInitExpression node)
            {
                var bindings = FilterMemberBindings(node.Bindings);

                if (bindings.Count == 0)
                {
                    throw new DException("未指定插入字段!");
                }

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
                            insertFields.Add(value);

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

                        writer.Parameter(kv.Value.Create());

                        insertFields.Add(tableInfo.ReadOrWrites[kv.Key]);
                    }

                    return node;
                }

                throw new DException("未指定插入字段!");
            }
        }

        /// <inheritdoc />
        public InsertVisitor(ExecuteVisitor visitor) : base(visitor)
        {
        }

        /// <summary>
        /// 行为。
        /// </summary>
        public ActionBehavior Behavior => ActionBehavior.Insert;

        /// <summary>
        /// 超时时间。
        /// </summary>
        public int? TimeOut { private set; get; }


        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.Insert && node.Method.DeclaringType == typeof(RepositoryExtentions);

        /// <inheritdoc />
        protected override Expression VisitOfSelect(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.TimeOut:

                    TimeOut += (int)node.Arguments[1].GetValueFromExpression();

                    base.Visit(node.Arguments[0]);

                    return node;
                case MethodCall.Insert:

                    var insertFields = new List<string>();

                    Expression objectExp = node.Arguments[0];

                    base.Visit(objectExp);

                    Workflow(() =>
                    {
                        var tableInfo = MakeTableInfo(objectExp.Type);

                        writer.Insert();

                        writer.Name(GetTableName(tableInfo));

                        var enumerator = insertFields.GetEnumerator();

                        if (enumerator.MoveNext())
                        {
                            writer.OpenBrace();

                            writer.Name(enumerator.Current);

                            while (enumerator.MoveNext())
                            {
                                writer.Delimiter();

                                writer.Name(enumerator.Current);
                            }

                            writer.CloseBrace();
                        }
                        else
                        {
                            throw new DException("未指定插入字段!");
                        }
                    }, () =>
                    {
                        using (var visitor = new InsertSelectVisitor(this, insertFields))
                        {
                            visitor.Startup(node.Arguments[1]);
                        }
                    });

                    return node;
                default:
                    return base.VisitOfSelect(node);
            }
        }
    }
}

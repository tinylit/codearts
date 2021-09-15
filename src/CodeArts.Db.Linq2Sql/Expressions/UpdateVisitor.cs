using CodeArts.Db.Exceptions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.Db.Expressions
{
    /// <summary>
    /// 更新访问器。
    /// </summary>
    public class UpdateVisitor : CoreVisitor
    {
        private bool buildWatchSql = false;
        private readonly ExecuteVisitor visitor;

        /// <inheritdoc />
        public UpdateVisitor(ExecuteVisitor visitor) : base(visitor)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.Update && node.Method.DeclaringType == Types.RepositoryExtentions;

        /// <inheritdoc />
        protected override void VisitOfLts(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.TimeOut:

                    visitor.SetTimeOut((int)node.Arguments[1].GetValueFromExpression());

                    base.Visit(node.Arguments[0]);

                    break;
                case MethodCall.WatchSql:
                    buildWatchSql = true;

                    Visit(node.Arguments[1]);

                    buildWatchSql = false;

                    Visit(node.Arguments[0]);

                    break;
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
                            WriteTableName(tableInfo, prefix);
                        }

                        writer.Set();

                        Visit(node.Arguments[1]);

                        if (settings.Engine == DatabaseEngine.SqlServer || settings.Engine == DatabaseEngine.Access)
                        {
                            writer.From();

                            WriteTableName(tableInfo, prefix);
                        }

                    }, () =>
                    {
                        Visit(objectExp);
                    });

                    break;
                default:
                    base.VisitOfLts(node);

                    break;
            }
        }

        /// <inheritdoc />
        protected override void VisitParameterLeavesIsObject(ParameterExpression node)
        {
            throw new DSyntaxErrorException("禁止原字段更新!");
        }

        /// <inheritdoc />
        protected override Expression VisitNew(NewExpression node)
        {
            var members = FilterMembers(node.Members);

            var enumerator = members.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var tableInfo = MakeTableInfo(node.Type);

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

                        VisitNewMember(memberInfo, node.Arguments[node.Members.IndexOf(memberInfo)]);
                    }
                    else
                    {
                        throw new DSyntaxErrorException($"字段“{memberInfo.Name}”不可写!");
                    }
                }

                foreach (var kv in tableInfo.Tokens)
                {
                    if (members.Any(x => x.Name == kv.Key))
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

                        VisitMemberBinding(binding);
                    }
                    else
                    {
                        throw new DSyntaxErrorException($"字段“{binding.Member.Name}”不可写!");
                    }
                }

                foreach (var kv in tableInfo.Tokens)
                {
                    if (bindings.Any(x => x.Member.Name == kv.Key))
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
        protected override void Constant(Type conversionType, object value)
        {
            if (buildWatchSql)
            {
                visitor.WatchSql((Action<CommandSql>)value);
            }
            else
            {
                base.Constant(conversionType, value);
            }
        }
    }
}

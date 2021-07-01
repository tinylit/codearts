using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.Db.Expressions
{
    /// <summary>
    /// 插入访问器。
    /// </summary>
    public class InsertVisitor : BaseVisitor
    {
        private readonly ExecuteVisitor visitor;

        private class InsertSelectVisitor : SelectVisitor
        {
            private readonly List<string> insertFields;

            public InsertSelectVisitor(BaseVisitor visitor, List<string> insertFields) : base(visitor)
            {
                this.insertFields = insertFields;
            }

            /// <inheritdoc />
            protected override void DefMemberAs(string field, string alias)
            {
                insertFields.Add(field);
            }

            /// <inheritdoc />
            protected override void DefMemberBindingAs(MemberBinding member, Type memberOfHostType)
            {
                insertFields.Add(GetMemberNaming(memberOfHostType, member.Member));
            }

            /// <inheritdoc />
            protected override void DefNewMemberAs(MemberInfo memberInfo, Type memberOfHostType)
            {
                insertFields.Add(GetMemberNaming(memberOfHostType, memberInfo));
            }
        }

        /// <inheritdoc />
        public InsertVisitor(ExecuteVisitor visitor) : base(visitor)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.Insert && node.Method.DeclaringType == Types.RepositoryExtentions;

        /// <inheritdoc />
        protected override void VisitOfLts(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.TimeOut:

                    visitor.SetTimeOut((int)node.Arguments[1].GetValueFromExpression());

                    base.Visit(node.Arguments[0]);

                    break;
                case MethodCall.Insert:

                    var insertFields = new List<string>();

                    Expression objectExp = node.Arguments[0];

                    base.Visit(objectExp);

                    Workflow(() =>
                    {
                        var tableInfo = MakeTableInfo(objectExp.Type);

                        writer.Insert();

                        WriteTableName(tableInfo, string.Empty);

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
                            throw new DException("未指定更新字段!");
                        }
                    }, () =>
                    {
                        using (var visitor = new InsertSelectVisitor(this, insertFields))
                        {
                            visitor.Startup(node.Arguments[1]);
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

using CodeArts.ORM.Exceptions;
using CodeArts.Runtime;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CodeArts.ORM.Visitors
{
    /// <summary>
    /// Join。
    /// </summary>
    public class JoinVisitor : ConditionVisitor
    {
        private class JoinSelectVisitor : SelectVisitor
        {
            public JoinSelectVisitor(BaseVisitor visitor) : base(visitor)
            {
            }

            protected override void VisitNewMember(MemberInfo memberInfo, Expression memberExp, Type memberOfHostType)
            {
                base.VisitNewMember(memberInfo, memberExp, memberOfHostType);

                writer.As(base.GetMemberNaming(memberOfHostType, memberInfo));
            }
        }

        private readonly BaseVisitor visitor;

        /// <inheritdoc />
        public JoinVisitor(BaseVisitor visitor) : base(visitor)
        {
            this.visitor = visitor;
        }

        private bool IsValidExpretion(Expression node)
        {
            if (node is MethodCallExpression methodCall)
            {
                if (methodCall.Method.Name == MethodCall.Join)
                {
                    return true;
                }

                if (methodCall.Method.Name == MethodCall.From)
                {
                    node = methodCall.Arguments[0];
                }
                else
                {
                    return false;
                }
            }

            while (node is MethodCallExpression method)
            {
                if (method.Method.Name == MethodCall.From)
                {
                    node = method.Arguments[0];

                    continue;
                }

                return false;
            }

            return true;
        }
        private bool IsPlainExpression(Expression node)
        {
            while (node is MethodCallExpression methodCall)
            {
                if (methodCall.Method.Name == MethodCall.From || methodCall.Method.Name == MethodCall.Where)
                {
                    node = methodCall.Arguments[0];

                    continue;
                }

                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override bool CanResolve(MethodCallExpression node) => node.Method.Name == MethodCall.Join;

        /// <inheritdoc />
        protected override Expression StartupCore(MethodCallExpression node)
        {
            if (!IsValidExpretion(node.Arguments[0]))
            {
                throw new NotSupportedException($"“Join”函数左连接仅允许出现“From”函数！");
            }

            if (IsPlainExpression(node.Arguments[1]))
            {
                return VisitPlainJoin(node);
            }

            return VisitComplexJoin(node);
        }

        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns></returns>
        protected override Expression VisitCondition(MethodCallExpression node)
        {
            base.UsingCondition(() => VisitBinaryIsConditionToVisit(node.Arguments[1]), whereIsNotEmpty =>
            {
                base.Visit(node.Arguments[0]);

                if (whereIsNotEmpty)
                {
                    writer.And();
                }
            });

            return node;
        }

        /// <summary>
        /// 普通Join。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitPlainJoin(MethodCallExpression node)
        {
            var sb = new StringBuilder();

            var rightNode = node.Arguments[1];

            base.Workflow(() =>
            {
                visitor.Visit(node.Arguments[0]);

                writer.Join();

                var tableInfo = base.MakeTableInfo(rightNode.Type);

                var prefix = base.GetEntryAlias(tableInfo.TableType, string.Empty);

                writer.NameWhiteSpace(GetTableName(tableInfo), prefix);

            }, () =>
            {
                writer.Write(" ON ");

                visitor.Visit(node.Arguments[2]);

                writer.Equal();

                base.Visit(node.Arguments[3]);

                base.Visit(node.Arguments[1]);
            });

            return node;
        }

        private Expression JoinWhereMethod(Expression node)
        {
            while (node.NodeType == ExpressionType.Call && node is MethodCallExpression methodCall)
            {
                if (methodCall.Method.Name == MethodCall.From)
                {
                    throw new DSyntaxErrorException($"在连接函数中，禁止使用“{methodCall.Method.Name}”函数!");
                }

                if (methodCall.Method.Name == MethodCall.Where)
                {
                    base.UsingCondition(() => base.Visit(methodCall.Arguments[1]), whereIsNotEmpty =>
                    {
                        if (whereIsNotEmpty)
                        {
                            writer.And();
                        }
                    });

                    node = methodCall.Arguments[0];

                    continue;
                }

                break;
            }

            return node;
        }

        /// <summary>
        /// 复杂Join。
        /// </summary>
        /// <returns></returns>
        protected virtual Expression VisitComplexJoin(MethodCallExpression node)
        {
            var sb = new StringBuilder();

            var rightNode = node.Arguments[1];

            base.Workflow(() =>
            {
                visitor.Visit(node.Arguments[0]);

                writer.Join();

                writer.OpenBrace();

                using (var visitor = new JoinSelectVisitor(this))
                {
                    visitor.Startup(rightNode);
                }

                writer.CloseBrace();

                writer.WhiteSpace();

                writer.Name(GetEntryAlias(rightNode.Type, string.Empty));

            }, () =>
            {
                writer.Write(" ON ");

                visitor.Visit(node.Arguments[2]);

                writer.Equal();

                base.Visit(node.Arguments[3]);

                rightNode = JoinWhereMethod(rightNode);
            });

            return node;
        }
    }
}

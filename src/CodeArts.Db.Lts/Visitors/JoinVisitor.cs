using CodeArts.Db.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.Join{TOuter, TInner, TKey, TResult}(IQueryable{TOuter}, System.Collections.Generic.IEnumerable{TInner}, Expression{Func{TOuter, TKey}}, Expression{Func{TInner, TKey}}, Expression{Func{TOuter, TInner, TResult}})"/>
    /// </summary>
    public class JoinVisitor : CoreVisitor
    {
        private class MyNewExpressionVisitor : ExpressionVisitor
        {
            private readonly Dictionary<MemberInfo, Expression> keyValues;

            public MyNewExpressionVisitor(Dictionary<MemberInfo, Expression> keyValues)
            {
                this.keyValues = keyValues;
            }

            protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
            {
                keyValues.Add(node.Member, node.Expression);

                return node;
            }

            protected override Expression VisitMemberInit(MemberInitExpression node)
            {
                foreach (var binding in node.Bindings)
                {
                    VisitMemberBinding(binding);
                }

                return node;
            }

            /// <inheritdoc />
            protected override Expression VisitNew(NewExpression node)
            {
                for (int i = 0; i < node.Members.Count; i++)
                {
                    keyValues.Add(node.Members[i], node.Arguments[i]);
                }

                return node;
            }
        }

        private bool buildNewEqual = false;

        private readonly SelectVisitor visitor;
        private readonly Dictionary<MemberInfo, Expression> joinExpressions = new Dictionary<MemberInfo, Expression>();

        /// <inheritdoc />
        public JoinVisitor(SelectVisitor visitor) : base(visitor, false, ConditionType.And)
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
        public override bool CanResolve(MethodCallExpression node)
            => node.Method.DeclaringType == Types.Queryable && node.Method.Name == MethodCall.Join && node.Arguments.Count == 5;

        /// <inheritdoc />
        protected override void StartupCore(MethodCallExpression node)
        {
            if (!IsValidExpretion(node.Arguments[0]))
            {
                throw new NotSupportedException($"“Join”函数左连接仅允许出现“From”函数！");
            }

            if (IsPlainExpression(node.Arguments[1]))
            {
                VisitPlainJoin(node);
            }
            else
            {
                VisitComplexJoin(node);
            }
        }

        private static bool IsNewEquals(Expression node)
        {
            switch (node)
            {
                case LambdaExpression lambda:
                    return IsNewEquals(lambda.Body);
                case UnaryExpression unary:
                    return IsNewEquals(unary.Operand);
                default:
                    return node.NodeType == ExpressionType.New || node.NodeType == ExpressionType.MemberInit;
            }
        }

        /// <summary>
        /// 普通Join。
        /// </summary>
        /// <returns></returns>
        protected virtual void VisitPlainJoin(MethodCallExpression node)
        {
            var sb = new StringBuilder();

            var rightNode = node.Arguments[1];

            Workflow(() =>
            {
                visitor.Visit(node.Arguments[0]);

                writer.Join();

                WriteTableName(rightNode.Type);

            }, () =>
            {
                writer.Write(" ON ");

                if (IsNewEquals(node.Arguments[2]))
                {
                    buildNewEqual = true;

                    base.Visit(node.Arguments[3]);

                    buildNewEqual = false;

                    base.Visit(node.Arguments[2]);
                }
                else
                {
                    visitor.Visit(node.Arguments[2]);

                    writer.Equal();

                    base.Visit(node.Arguments[3]);
                }

                base.Visit(node.Arguments[1]);
            });
        }

        /// <inheritdoc />
        protected override void VisitLambdaBody(Expression node)
        {
            if (buildNewEqual)
            {
                var visitor = new MyNewExpressionVisitor(joinExpressions);

                visitor.Visit(node);
            }
            else
            {
                base.VisitLambdaBody(node);
            }
        }

        /// <inheritdoc />
        protected override void MemberDelimiter() => writer.And();

        /// <inheritdoc />
        protected internal override void VisitNewMember(MemberInfo memberInfo, Expression node)
        {
            if (!joinExpressions.TryGetValue(memberInfo, out Expression expression))
            {
                throw new DSyntaxErrorException($"未找到{memberInfo.Name}的关联关系！");
            }

            base.VisitNewMember(memberInfo, node);

            writer.Equal();

            visitor.Visit(expression);
        }

        /// <inheritdoc />
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            if (!joinExpressions.TryGetValue(node.Member, out Expression expression))
            {
                throw new DSyntaxErrorException($"未找到{node.Member.Name}的关联关系！");
            }

            var result = base.VisitMemberAssignment(node);

            writer.Equal();

            visitor.Visit(expression);

            return result;
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
                    Workflow(whereIsNotEmpty =>
                    {
                        if (whereIsNotEmpty)
                        {
                            writer.And();
                        }

                    }, () =>
                    {
                        using (var visitor = new WhereVisitor(this))
                        {
                            visitor.Startup(methodCall.Arguments[1]);
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
        protected virtual void VisitComplexJoin(MethodCallExpression node)
        {
            var sb = new StringBuilder();

            var rightNode = node.Arguments[1];

            Workflow(() =>
            {
                visitor.Visit(node.Arguments[0]);

                writer.Join();

                writer.OpenBrace();

                using (var visitor = new SelectVisitor(this))
                {
                    visitor.Startup(rightNode);
                }

                writer.CloseBrace();

                writer.WhiteSpace();

                writer.Name(GetEntryAlias(rightNode.Type, string.Empty));

            }, () =>
            {
                writer.Write(" ON ");

                if (IsNewEquals(node.Arguments[2]))
                {
                    buildNewEqual = true;

                    base.Visit(node.Arguments[3]);

                    buildNewEqual = false;

                    base.Visit(node.Arguments[2]);
                }
                else
                {
                    visitor.Visit(node.Arguments[2]);

                    writer.Equal();

                    base.Visit(node.Arguments[3]);
                }

                rightNode = JoinWhereMethod(rightNode);
            });
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                joinExpressions.Clear();
            }

            base.Dispose(disposing);
        }
    }
}

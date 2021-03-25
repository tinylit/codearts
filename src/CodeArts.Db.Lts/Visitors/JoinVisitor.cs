using CodeArts.Db.Exceptions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace CodeArts.Db.Lts.Visitors
{
    /// <summary>
    /// <see cref="Queryable.Join{TOuter, TInner, TKey, TResult}(IQueryable{TOuter}, System.Collections.Generic.IEnumerable{TInner}, Expression{Func{TOuter, TKey}}, Expression{Func{TInner, TKey}}, Expression{Func{TOuter, TInner, TResult}})"/>
    /// </summary>
    public class JoinVisitor : CoreVisitor
    {
        private readonly SelectVisitor visitor;

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

                visitor.Visit(node.Arguments[2]);

                writer.Equal();

                base.Visit(node.Arguments[3]);

                base.Visit(node.Arguments[1]);
            });
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

            Workflow((Action)(() =>
            {
                visitor.Visit(node.Arguments[0]);

                writer.Join();

                writer.OpenBrace();


/* 项目“CodeArts.Db.Lts (netstandard2.0)”的未合并的更改
在此之前:
                using (var visitor = new SelectVisitor(this))
在此之后:
                using (var visitor = new Visitors.SelectVisitor(this))
*/

/* 项目“CodeArts.Db.Lts (net45)”的未合并的更改
在此之前:
                using (var visitor = new SelectVisitor(this))
在此之后:
                using (var visitor = new Visitors.SelectVisitor(this))
*/

/* 项目“CodeArts.Db.Lts (net40)”的未合并的更改
在此之前:
                using (var visitor = new SelectVisitor(this))
在此之后:
                using (var visitor = new Visitors.SelectVisitor(this))
*/

/* 项目“CodeArts.Db.Lts (netstandard2.1)”的未合并的更改
在此之前:
                using (var visitor = new SelectVisitor(this))
在此之后:
                using (var visitor = new Visitors.SelectVisitor(this))
*/
                using (var visitor = new SelectVisitor(this))
                {
                    visitor.Startup(rightNode);
                }

                writer.CloseBrace();

                writer.WhiteSpace();

                writer.Name(GetEntryAlias(rightNode.Type, string.Empty));

            }), () =>
            {
                writer.Write(" ON ");

                visitor.Visit(node.Arguments[2]);

                writer.Equal();

                base.Visit(node.Arguments[3]);

                rightNode = JoinWhereMethod(rightNode);
            });
        }
    }
}

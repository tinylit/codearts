using CodeArts.Db.Exceptions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Expressions
{
    /// <summary>
    /// <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{System.Func{TSource, bool}})"/>、<seealso cref="Queryable.TakeWhile{TSource}(IQueryable{TSource}, Expression{System.Func{TSource, bool}})"/>、<seealso cref="Queryable.SkipWhile{TSource}(IQueryable{TSource}, Expression{System.Func{TSource, bool}})"/>的第二个参数。
    /// </summary>
    public class WhereVisitor : BaseVisitor
    {
        private readonly BaseVisitor visitor;

        /// <inheritdoc />
        public WhereVisitor(BaseVisitor visitor) : base(visitor, false)
        {
            this.visitor = visitor;
        }

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == Types.Queryable)
            {
                VisitCore(node);

                return node;
            }

            return visitor.Visit(node);
        }

        /// <inheritdoc />
        protected override void VisitCore(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case MethodCall.Any:
                    using (var visitor = new NestedAnyVisitor(this.visitor))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.All:
                    using (var visitor = new NestedAllVisitor(this.visitor))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Contains:
                    using (var visitor = new NestedContainsVisitor(this.visitor))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case MethodCall.Select:
                    using (var visitor = new SelectVisitor(this.visitor))
                    {
                        writer.OpenBrace();

                        visitor.Startup(node);

                        writer.CloseBrace();
                    }
                    break;
                default:
                    if (node.Type.IsValueType || node.Type == Types.String || !node.Type.IsQueryable())
                    {
                        goto case MethodCall.Select;
                    }

                    visitor.Visit(node);

                    break;
            }
        }

        /// <inheritdoc />
        protected override void VisitLambdaBody(Expression node) => VisitBinaryIsConditionToVisit(node);

        /// <inheritdoc />
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            base.VisitBinaryIsConditionToVisit(node);

            return node;
        }

        /// <inheritdoc />
        protected override void VisitBinaryIsBoolean(BinaryExpression node)
        {
            if (node.Left.NodeType == ExpressionType.Coalesce || node.Right.NodeType == ExpressionType.Coalesce)
            {
                VisitSkipIsCondition(Done);
            }
            else
            {
                base.VisitBinaryIsBoolean(node);
            }

            void Done()
            {
                if (node.Left.NodeType == ExpressionType.Coalesce && node.Right.NodeType == ExpressionType.Coalesce)
                {
                    VisitBinaryIsBooleanDoubleCoalesce(node.Left, node.NodeType, node.Right);
                }
                else if (node.Left.NodeType == ExpressionType.Coalesce)
                {
                    VisitBinaryIsBooleanLeftCoalesce(node.Left, node.NodeType, node.Right);
                }
                else
                {
                    VisitBinaryIsBooleanRightCoalesce(node.Left, node.NodeType, node.Right);
                }
            }
        }

        /// <summary>
        /// 1==(x.status??1)
        /// </summary>
        protected virtual void VisitBinaryIsBooleanRightCoalesce(Expression left, ExpressionType expressionType, Expression right)
        {
            bool flag = false;

            int closeBraceCount = 0;

            int index, length, appendAt;

            while ((right is BinaryExpression binary))
            {
                if (binary.NodeType != ExpressionType.Coalesce)
                {
                    throw new DSyntaxErrorException("空合并运算中，不允许出现非空合并运算符！");
                }

                right = binary.Right;

                if (IsSkip(left, expressionType, binary.Left, writer.IsReverseCondition))
                {
                    continue;
                }

                index = writer.Length;
                length = writer.Length;
                appendAt = writer.AppendAt;

                if (appendAt > -1)
                {
                    index -= (index - appendAt);
                }

                VisitBinaryIsBoolean(left, expressionType, binary.Left);

                if (writer.Length == length)
                {
                    continue;
                }

                writer.AppendAt = index;

                if (flag)
                {
                    writer.And();
                }
                else
                {
                    flag = true;
                }

                if (appendAt > -1)
                {
                    appendAt += writer.Length - length;
                }

                writer.AppendAt = appendAt;

                closeBraceCount++;

                writer.Or();

                writer.OpenBrace();

                VisitTail(binary.Left);

                writer.IsNull();
            }

            if (IsSkip(left, expressionType, right, writer.IsReverseCondition))
            {
                goto label_CloseBrace;
            }

            index = writer.Length;
            length = writer.Length;
            appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                index -= (index - appendAt);
            }

            VisitBinaryIsBoolean(left, expressionType, right);

            if (writer.Length == length)
            {
                goto label_CloseBrace;
            }

            writer.AppendAt = index;

            if (flag)
            {
                writer.And();
            }

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;

            label_CloseBrace:

            while (closeBraceCount-- > 0)
            {
                writer.CloseBrace();
            }
        }

        /// <summary>
        /// (x.status??1)==1
        /// </summary>
        protected virtual void VisitBinaryIsBooleanLeftCoalesce(Expression left, ExpressionType expressionType, Expression right)
        {
            bool flag = false;

            int closeBraceCount = 0;

            int index, length, appendAt;

            while ((left is BinaryExpression binary))
            {
                if (binary.NodeType != ExpressionType.Coalesce)
                {
                    throw new DSyntaxErrorException("空合并运算中，不允许出现非空合并运算符！");
                }

                left = binary.Right;

                if (IsSkip(binary.Left, expressionType, right, writer.IsReverseCondition))
                {
                    continue;
                }

                index = writer.Length;
                length = writer.Length;
                appendAt = writer.AppendAt;

                if (appendAt > -1)
                {
                    index -= (index - appendAt);
                }

                VisitBinaryIsBoolean(binary.Left, expressionType, right);

                if (writer.Length == length)
                {
                    continue;
                }

                writer.AppendAt = index;

                if (flag)
                {
                    writer.And();
                }
                else
                {
                    flag = true;
                }

                if (appendAt > -1)
                {
                    appendAt += writer.Length - length;
                }

                writer.AppendAt = appendAt;

                closeBraceCount++;

                writer.Or();

                writer.OpenBrace();

                VisitTail(binary.Left);

                writer.IsNull();
            }

            if (IsSkip(left, expressionType, right, writer.IsReverseCondition))
            {
                goto label_CloseBrace;
            }

            index = writer.Length;
            length = writer.Length;
            appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                index -= (index - appendAt);
            }

            VisitBinaryIsBoolean(left, expressionType, right);

            if (writer.Length == length)
            {
                goto label_CloseBrace;
            }

            writer.AppendAt = index;

            if (flag)
            {
                writer.And();
            }

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;

            label_CloseBrace:

            while (closeBraceCount-- > 0)
            {
                writer.CloseBrace();
            }
        }

        /// <summary>
        /// (x.old_status??1)==(x.status??1)
        /// </summary>
        protected virtual void VisitBinaryIsBooleanDoubleCoalesce(Expression left, ExpressionType expressionType, Expression right)
        {
            bool flag = false;

            int closeBraceCount = 0;

            int index, length, appendAt;

            while ((left is BinaryExpression binary))
            {
                if (binary.NodeType != ExpressionType.Coalesce)
                {
                    throw new DSyntaxErrorException("空合并运算中，不允许出现非空合并运算符！");
                }

                left = binary.Right;

                index = writer.Length;
                length = writer.Length;
                appendAt = writer.AppendAt;

                if (appendAt > -1)
                {
                    index -= (index - appendAt);
                }

                VisitBinaryIsBooleanRightCoalesce(binary.Left, expressionType, right);

                if (writer.Length == length)
                {
                    continue;
                }

                writer.AppendAt = index;

                if (flag)
                {
                    writer.And();
                }
                else
                {
                    flag = true;
                }

                if (appendAt > -1)
                {
                    appendAt += writer.Length - length;
                }

                writer.AppendAt = appendAt;

                closeBraceCount++;

                writer.Or();

                writer.OpenBrace();

                VisitTail(binary.Left);

                writer.IsNull();
            }

            index = writer.Length;
            length = writer.Length;
            appendAt = writer.AppendAt;

            if (appendAt > -1)
            {
                index -= (index - appendAt);
            }

            VisitBinaryIsBooleanRightCoalesce(left, expressionType, right);

            if (writer.Length == length)
            {
                goto label_CloseBrace;
            }

            writer.AppendAt = index;

            if (flag)
            {
                writer.And();
            }

            if (appendAt > -1)
            {
                appendAt += writer.Length - length;
            }

            writer.AppendAt = appendAt;

            label_CloseBrace:

            while (closeBraceCount-- > 0)
            {
                writer.CloseBrace();
            }
        }

        private static bool IsSkip(Expression left, ExpressionType type, Expression right, bool flag)
        {
            if (left.NodeType != ExpressionType.Constant || right.NodeType != ExpressionType.Constant)
            {
                return false;
            }

            var body = Expression.MakeBinary(type, left, right);

            var lambdaEx = Expression.Lambda<Func<bool>>(body);

            return lambdaEx.Compile().Invoke() != flag;
        }

        /// <inheritdoc />
        protected override void VisitBinaryIsCoalesceLeftIsNormal(BinaryExpression node)
        {
            if (node.Left.Type == typeof(bool?))
            {
                int startIndex = writer.Length;

                VisitSkipIsCondition(node.Left);

                int length = writer.Length - startIndex;

                writer.Equal();

                writer.BooleanTrue();

                writer.Or();

                writer.OpenBrace();

                writer.Write(writer.ToString(startIndex, length));

                writer.IsNull();

                Workflow(hasValue =>
                {
                    if (hasValue)
                    {
                        writer.And();
                    }

                }, () => VisitTail(node.Right));

                writer.CloseBrace();
            }
            else
            {
                base.VisitBinaryIsCoalesceLeftIsNormal(node);
            }
        }

        /// <inheritdoc />
        protected override void VisitConditionalNormal(ConditionalExpression node)
        {
            writer.OpenBrace();

            VisitTail(node.Test);

            writer.And();

            VisitTail(node.IfTrue);

            writer.CloseBrace();

            writer.Or();

            writer.ReverseCondition(() => VisitTail(node.Test));

            writer.And();

            VisitTail(node.IfTrue);

            writer.CloseBrace();
        }

        /// <inheritdoc />
        protected override void VisitNot(UnaryExpression node)
        {
            if (node.Operand.IsBoolean())
            {
                writer.ReverseCondition(() => VisitBinaryIsConditionToVisit(node.Operand));
            }
            else
            {
                base.VisitNot(node);
            }
        }
    }
}

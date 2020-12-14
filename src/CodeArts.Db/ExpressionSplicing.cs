namespace System.Linq.Expressions
{
    /// <summary>
    /// 表达式拼接。
    /// </summary>
    public static class ExpressionSplicing
    {
        /// <summary>
        /// 获取始终为真的条件。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Expression<Func<T, bool>> True<T>() => x => true;

        /// <summary>
        /// 获取始终为真的条件。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <returns></returns>
        public static Expression<Func<T, bool>> False<T>() => x => false;

        /// <summary>
        /// 且。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="leftNode">左节点。</param>
        /// <param name="rightNode">右节点。</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> leftNode, Expression<Func<T, bool>> rightNode)
        {
            if (leftNode is null)
            {
                return rightNode;
            }

            if (rightNode is null)
            {
                return leftNode;
            }

            if (leftNode.Body.NodeType == ExpressionType.Constant && leftNode.Body is ConstantExpression constant)
            {
                if (Equals(constant.Value, true))
                {
                    return rightNode;
                }

                return leftNode;
            }

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftNode.Body, new ReplaceExpressionVisitor(rightNode.Parameters[0], leftNode.Parameters[0]).Visit(rightNode.Body)), leftNode.Parameters);
        }

        /// <summary>
        /// 或。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="leftNode">左节点。</param>
        /// <param name="rightNode">右节点。</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> leftNode, Expression<Func<T, bool>> rightNode)
        {
            if (leftNode is null)
            {
                return rightNode;
            }

            if (rightNode is null)
            {
                return leftNode;
            }

            if (leftNode.Body.NodeType == ExpressionType.Constant && leftNode.Body is ConstantExpression constant)
            {
                if (Equals(constant.Value, false))
                {
                    return rightNode;
                }

                return leftNode;
            }

            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(leftNode.Body, new ReplaceExpressionVisitor(rightNode.Parameters[0], leftNode.Parameters[0]).Visit(rightNode.Body)), leftNode.Parameters);
        }

        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldExpression;
            private readonly Expression _newExpression;

            public ReplaceExpressionVisitor(Expression oldExpression, Expression newExpression)
            {
                _oldExpression = oldExpression;
                _newExpression = newExpression;
            }
            public override Expression Visit(Expression node)
            {
                if (_oldExpression == node)
                {
                    return base.Visit(_newExpression);
                }

                return base.Visit(node);
            }
        }
    }
}

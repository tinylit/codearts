using System;
using System.Linq.Expressions;

namespace CodeArts.Db
{
    /// <summary>
    /// 表达式拓展类。
    /// </summary>
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// 是否为boolean表达式。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsBoolean(this UnaryExpression node)
        {
            if (node is null)
            {
                return false;
            }

            return node.Operand.IsBoolean();
        }
        /// <summary>
        /// 是否为boolean表达式。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsBoolean(this Expression node)
        {
            if (node is null)
            {
                return false;
            }

            return node.Type.IsBoolean();
        }

        /// <summary>
        /// 是否是HasValue属性。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsHasValue(this MemberExpression node)
        {
            if (node is null)
            {
                return false;
            }

            return node.Member.Name == "HasValue";
        }

        /// <summary>
        /// 是否是Value属性。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsValue(this MemberExpression node)
        {
            if (node is null)
            {
                return false;
            }

            return node.Member.Name == "Value";
        }
        /// <summary>
        /// 是否是Length属性。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsLength(this MemberExpression node)
        {
            if (node is null)
            {
                return false;
            }

            return node.Member.Name == "Length";
        }
        /// <summary>
        /// 是否为可空类型。
        /// </summary>
        /// <param name="member">表达式。</param>
        /// <returns></returns>
        internal static bool IsNullable(this MemberExpression member)
        {
            if (member is null)
            {
                return false;
            }

            return member.Type.IsNullable();
        }

        /// <summary>
        /// 是否为陈述语句。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsPredicate(this UnaryExpression node)
        {
            if (node is null)
            {
                return false;
            }

            return node.Operand.IsPredicate();
        }
        /// <summary>
        /// 是否为陈述语句。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsPredicate(this Expression node)
        {
            if (node is null)
            {
                return false;
            }

            switch (node.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.Not:
                case ExpressionType.Call:
                    return node.IsBoolean();
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    return true;
                default:
                    return false;
            }
        }
        /// <summary>
        /// 是否为变量类型。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsVariable(this MemberExpression node)
        {
            if (node is null)
            {
                return false;
            }

            return node.Expression.IsVariable();
        }

        /// <summary>
        /// 是否为变量类型。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsVariable(this Expression node)
        {
            if (node is null)
            {
                return false;
            }

            if (node.NodeType == ExpressionType.MemberAccess)
            {
                return ((MemberExpression)node).IsVariable();
            }

            return node.NodeType == ExpressionType.Constant;
        }
        /// <summary>
        /// 获取操作符。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static string GetOperator(this UnaryExpression node)
        {
            if (node is null)
            {
                return string.Empty;
            }

            switch (node.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return "-";
                case ExpressionType.UnaryPlus:
                    return "+";
                case ExpressionType.Not:
                    return node.IsBoolean() ? "NOT" : "~";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Where条件（不包含And或Or）。
        /// </summary>
        /// <param name="nodeType">节点类型。</param>
        /// <returns></returns>
        internal static ExpressionType ReverseWhere(this ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.GreaterThan:
                    return ExpressionType.LessThanOrEqual;
                case ExpressionType.GreaterThanOrEqual:
                    return ExpressionType.LessThan;
                case ExpressionType.LessThan:
                    return ExpressionType.GreaterThanOrEqual;
                case ExpressionType.LessThanOrEqual:
                    return ExpressionType.GreaterThan;

                case ExpressionType.Equal:
                    return ExpressionType.NotEqual;
                case ExpressionType.NotEqual:
                    return ExpressionType.Equal;
            }
            return nodeType;
        }

        /// <summary>
        /// 获取操作符。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <param name="nodeType">节点类型。</param>
        /// <returns></returns>
        internal static string GetOperator(this BinaryExpression node, ExpressionType? nodeType = null)
        {
            if (node is null)
            {
                return string.Empty;
            }

            switch (nodeType ?? node.NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.And when node.Left.IsBoolean():
                    return " AND ";
                case ExpressionType.OrElse:
                case ExpressionType.Or when node.Left.IsBoolean():
                    return " OR ";
                default:
                    return GetOperator(nodeType ?? node.NodeType);
            }
        }
        /// <summary>
        /// 获取操作符。
        /// </summary>
        /// <param name="expressionType">表达式类型。</param>
        /// <returns></returns>
        internal static string GetOperator(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.And:
                    return " & ";
                case ExpressionType.Or:
                    return " | ";
                case ExpressionType.Equal:
                    return " = ";
                case ExpressionType.NotEqual:
                    return " <> ";
                case ExpressionType.LessThan:
                    return " < ";
                case ExpressionType.LessThanOrEqual:
                    return " <= ";
                case ExpressionType.GreaterThan:
                    return " > ";
                case ExpressionType.GreaterThanOrEqual:
                    return " >= ";
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return " + ";
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return " - ";
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return " * ";
                case ExpressionType.Divide:
                    return " / ";
                case ExpressionType.Modulo:
                    return " % ";
                case ExpressionType.ExclusiveOr:
                    return " ^ ";
                case ExpressionType.LeftShift:
                    return " << ";
                case ExpressionType.RightShift:
                    return " >> ";
                default:
                    throw new NotSupportedException();
            }
        }
        /// <summary>
        /// 获取表达式值。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static object GetValueFromExpression(this Expression node)
        {
            if (node is null)
            {
                return null;
            }

            return Expression.Lambda(node).Compile().DynamicInvoke();
        }

        /// <summary>
        /// 获取表达式成员名称。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static string GetPropertyMemberNameFromExpression(this MemberExpression node)
        {
            if (node is null)
            {
                return string.Empty;
            }

            return node.Member.Name;
        }
        /// <summary>
        /// 获取表达式成员名称。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static string GetPropertyMemberNameFromExpression(this Expression node) => node.GetMemberExpression().GetPropertyMemberNameFromExpression();
        /// <summary>
        /// 获取表达式属性名称。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static string GetPropertyNameFromExpression(this Expression node)
        {
            if (node is null)
            {
                return null;
            }

            switch (node.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return (node as MemberExpression).Member.Name;
                case ExpressionType.Parameter:
                    return (node as ParameterExpression).Name;
                case ExpressionType.Lambda:
                    return ((LambdaExpression)node).Body.GetPropertyNameFromExpression();
                default:
                    if (node is UnaryExpression unary)
                        return unary.Operand.GetPropertyNameFromExpression();
                    return null;
            }
        }
        /// <summary>
        /// 获取成员表达式。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static MemberExpression GetMemberExpression(this ConditionalExpression node)
        {
            if (node is null)
            {
                return null;
            }

            return node.Test.GetMemberExpression() ??
                node.IfTrue.GetMemberExpression() ??
                node.IfFalse.GetMemberExpression();
        }
        /// <summary>
        /// 获取成员表达式。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static MemberExpression GetMemberExpression(this LambdaExpression node)
        {
            if (node is null)
            {
                return null;
            }

            return node.Body.GetMemberExpression();
        }
        /// <summary>
        /// 获取成员表达式。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static MemberExpression GetMemberExpression(this UnaryExpression node)
        {
            if (node is null)
            {
                return null;
            }

            return node.Operand.GetMemberExpression();
        }
        /// <summary>
        /// 获取成员表达式。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static MemberExpression GetMemberExpression(this BinaryExpression node)
        {
            if (node is null)
            {
                return null;
            }

            return node.Left.GetMemberExpression() ?? node.Right.GetMemberExpression();
        }
        /// <summary>
        /// 获取成员表达式。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static MemberExpression GetMemberExpression(this Expression node)
        {
            if (node is null)
            {
                return null;
            }

            switch (node.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return node as MemberExpression;
                case ExpressionType.Lambda:
                    return ((LambdaExpression)node).GetMemberExpression();
                case ExpressionType.Conditional:
                    return ((ConditionalExpression)node).GetMemberExpression();
            }
            switch (node)
            {
                case UnaryExpression unary:
                    return unary.GetMemberExpression();
                case BinaryExpression binary:
                    return binary.GetMemberExpression();
            }
            return null;
        }
        /// <summary>
        /// 是否继承或泛型包含声明类型。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <param name="declaringType">声明类型。</param>
        /// <returns></returns>
        internal static bool GetDeclaringTypeExpression(this Expression node, Type declaringType)
        {
            if (node is null)
            {
                return false;
            }

            switch (node.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return node.Type.IsDeclaringType(declaringType) || ((MemberExpression)node).Expression.Type.IsDeclaringType(declaringType);
                case ExpressionType.Call:
                    foreach (var item in ((MethodCallExpression)node).Arguments)
                    {
                        if (item.Type.IsDeclaringType(declaringType))
                        {
                            return true;
                        }
                    }
                    return false;
                default:
                    if (node is BinaryExpression binary)
                    {
                        return binary.Left.GetDeclaringTypeExpression(declaringType) || binary.Right.GetDeclaringTypeExpression(declaringType);
                    }
                    return false;
            }
        }
    }
}

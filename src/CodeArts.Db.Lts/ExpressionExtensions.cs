using System;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts
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
            if (node is null || node.Expression is null)
            {
                return false;
            }

            return node.Member.Name == "HasValue" && node.Expression.IsNullable();
        }

        /// <summary>
        /// 是否是Value属性。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsValue(this MemberExpression node)
        {
            if (node is null || node.Expression is null)
            {
                return false;
            }

            return node.Member.Name == "Value" && node.Expression.IsNullable();
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

            return node.Member.Name == "Length" && node.Member.DeclaringType == Types.String;
        }

        /// <summary>
        /// 是否为可空类型。
        /// </summary>
        /// <param name="member">表达式。</param>
        /// <returns></returns>
        internal static bool IsNullable(this Expression member)
        {
            if (member is null)
            {
                return false;
            }

            return member.Type.IsNullable();
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
                default:
                    return nodeType;
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
                case ExpressionType.Not:
                case ExpressionType.OnesComplement:
                    return "~";
                case ExpressionType.UnaryPlus:
                    return "+";
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return "-";
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

            switch (node)
            {
                case ConstantExpression constant:
                    return constant.Value;
                case MemberExpression member when member.Expression is ConstantExpression constant:
                    return constant.Value;
                case LambdaExpression lambda when lambda.Body is ConstantExpression constant:
                    return constant.Value;
                case LambdaExpression lambda when lambda.Parameters.Count > 0:
                    throw new NotSupportedException();
                case LambdaExpression lambda:
                    return lambda.Compile().DynamicInvoke();
                default:
                    return Expression.Lambda(node).Compile().DynamicInvoke();
            }
        }

        /// <summary>
        /// 是否是<see cref="IGrouping{TKey, TElement}"/>
        /// </summary>
        /// <returns></returns>
        internal static bool IsGrouping(this Expression node)
        {
            switch (node)
            {
                case ParameterExpression parameter:
                    return parameter.Type.IsGrouping();
                case MethodCallExpression method:
                    return IsGrouping(method.Arguments[0]);
                case MemberExpression member when member.Expression is null:
                    return member.Type.IsGrouping();
                case MemberExpression member:
                    return member.Member.Name == "Key" ? member.Type.IsClass && !(member.Type == Types.String || member.Type == Types.Version) && member.Expression.Type.IsGrouping() : member.Type.IsGrouping();
                default:
                    return false;
            }
        }
    }
}

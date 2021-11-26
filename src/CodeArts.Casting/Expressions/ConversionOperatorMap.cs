using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Casting.Expressions
{
    /// <summary>
    /// 显示转换和隐式转换。
    /// </summary>
    public class ConversionOperatorMap : SimpleExpression
    {
        private readonly string _operatorName;

        private static MethodInfo GetConversionOperator(string operatorName, Type sourceType, Type destinationType)
        {
            foreach (MethodInfo sourceMethod in sourceType.GetMember(operatorName, MemberTypes.Method, MapExtensions.StaticFlags))
            {
                if (destinationType.IsAssignableFrom(sourceMethod.ReturnType))
                {
                    return sourceMethod;
                }
            }

            foreach (MethodInfo sourceMethod in destinationType.GetMember(operatorName, MemberTypes.Method, MapExtensions.StaticFlags))
            {
                if (sourceMethod.GetParameters().All(x => x.ParameterType.IsAssignableFrom(sourceType)))
                {
                    return sourceMethod;
                }
            }

            throw new InvalidCastException();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="operatorName">操作符名称。</param>
        public ConversionOperatorMap(string operatorName) => _operatorName = operatorName;

        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type conversionType)
        {
            foreach (MethodInfo sourceMethod in sourceType.GetMember(_operatorName, MemberTypes.Method, MapExtensions.StaticFlags))
            {
                if (conversionType.IsAssignableFrom(sourceMethod.ReturnType))
                {
                    return true;
                }
            }

            foreach (MethodInfo sourceMethod in conversionType.GetMember(_operatorName, MemberTypes.Method, MapExtensions.StaticFlags))
            {
                if (sourceMethod.GetParameters().All(x => x.ParameterType.IsAssignableFrom(sourceType)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceType">原类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="sourceExpression">数据源表达式。</param>
        /// <returns></returns>
        protected override Expression ToSolve(Type sourceType, Type conversionType, Expression sourceExpression)
            => Expression.Call(GetConversionOperator(_operatorName, sourceType, conversionType), sourceExpression);
    }
}

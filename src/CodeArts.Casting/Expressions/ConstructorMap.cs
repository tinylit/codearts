using CodeArts.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Casting.Expressions
{
    /// <summary>
    /// 构造器映射。
    /// </summary>
    public class ConstructorMap : SimpleExpression
    {
        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type conversionType)
            => conversionType.GetConstructors()
                .Any(x => x.GetParameters()
                        .All(y => y.ParameterType.IsAssignableFrom(sourceType))
                );

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceType">原类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="sourceExpression">数据源表达式。</param>
        /// <returns></returns>
        protected override Expression ToSolve(Type sourceType, Type conversionType, Expression sourceExpression)
        {
            var constructorInfo = conversionType.GetConstructors()
                .First(x => x.GetParameters()
                        .All(y => y.ParameterType.IsAssignableFrom(sourceType))
                );

            return Expression.New(constructorInfo, sourceExpression);
        }
    }
}

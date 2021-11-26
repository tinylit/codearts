using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Casting.Expressions
{
    /// <summary>
    /// 类型转换。
    /// </summary>
    public class ConvertMap : SimpleExpression
    {
        private static bool IsPrimitive(Type type) => type.IsPrimitive || type == typeof(string) || type == typeof(decimal);

        /// <summary>
        /// 能否解决。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type conversionType) => 
            (sourceType == typeof(string) && conversionType == typeof(DateTime)) ||
            (IsPrimitive(sourceType) && IsPrimitive(conversionType));

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceType">原类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="sourceExpression">数据源表达式。</param>
        /// <returns></returns>
        protected override Expression ToSolve(Type sourceType, Type conversionType, Expression sourceExpression)
        {
            var convertMethod = typeof(Convert).GetMethod("To" + conversionType.Name, new[] { sourceType });

            return Expression.Call(convertMethod, sourceExpression);
        }
    }
}

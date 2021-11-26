using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Casting.Expressions
{
    using static Expression;

    /// <summary>
    /// <see cref="KeyValuePair{TKey, TValue}"/>
    /// </summary>
    public class KeyValueMap : MapExpression
    {
        private readonly static Type KeyValueType = typeof(KeyValuePair<,>);

        private static bool IsKeyValue(Type conversionType) => conversionType.IsGenericType && conversionType.GetGenericTypeDefinition() == KeyValueType;

        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type conversionType)
            => IsKeyValue(sourceType) && IsKeyValue(conversionType);

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceType">原类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="configuration">配置文件。</param>
        /// <param name="sourceExpression">数据源表达式。</param>
        /// <returns></returns>
        protected override Expression ToSolve(Type sourceType, Type conversionType, IMapConfiguration configuration, Expression sourceExpression)
        {
            var sourceGenericArguments = sourceType.GetGenericArguments();
            var conversionGenericArguments = conversionType.GetGenericArguments();

            Expression keyExp = sourceGenericArguments[0] == conversionGenericArguments[0]
                ? Property(sourceExpression, "Key")
                : configuration.Map(Property(sourceExpression, "Key"), conversionGenericArguments[0]);
            Expression valueExp = sourceGenericArguments[1] == conversionGenericArguments[1]
                ? Property(sourceExpression, "Value")
                : configuration.Map(Property(sourceExpression, "Value"), conversionGenericArguments[1]);

            return New(conversionType.GetConstructor(conversionGenericArguments), new Expression[] { keyExp, valueExp });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Casting.Expressions
{
    using static Expression;

    /// <summary>
    /// 字符串转枚举。
    /// </summary>
    public class StringToEnumMap : SimpleExpression
    {
        private static readonly MethodInfo EqualsMethod = typeof(StringToEnumMap).GetMethod(nameof(StringCompareOrdinalIgnoreCase), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo ParseMethod = typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) });
        private static readonly MethodInfo IsNullOrEmptyMethod = typeof(string).GetMethod("IsNullOrEmpty");
        private static bool StringCompareOrdinalIgnoreCase(string x, string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        
        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type conversionType)
            => conversionType.IsEnum && sourceType == typeof(string);

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceType">原类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="sourceExpression">数据源表达式。</param>
        /// <returns></returns>
        protected override Expression ToSolve(Type sourceType, Type conversionType, Expression sourceExpression)
        {
            List<SwitchCase> switchCases = new List<SwitchCase>();
            foreach (var memberInfo in conversionType.GetFields(MapExtensions.StaticFlags))
            {
                var attribute = (EnumMemberAttribute)Attribute.GetCustomAttribute(memberInfo, typeof(EnumMemberAttribute));
#if NET461_OR_GREATER || NETSTANDARD2_0_OR_GREATER
                if (attribute.IsValueSetExplicitly)
#else
                if (attribute.Value != null)
#endif
                {
                    switchCases.Add(SwitchCase(Constant(memberInfo.GetRawConstantValue(), conversionType), Constant(attribute.Value)));
                }
            }

            var enumParseExp = Convert(Call(ParseMethod, Constant(conversionType), sourceExpression, Constant(true)), conversionType);

            return Condition(Call(IsNullOrEmptyMethod, sourceExpression), Default(conversionType), Switch(sourceExpression, enumParseExp, EqualsMethod, switchCases)); ;
        }
    }
}

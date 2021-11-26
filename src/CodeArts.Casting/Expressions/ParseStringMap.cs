using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Casting.Expressions
{
    using static Expression;

    /// <summary>
    /// 解析字符串。
    /// </summary>
    public class ParseStringMap : SimpleExpression
    {
        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type conversionType)
            => sourceType == typeof(string) && (conversionType == typeof(Guid) || conversionType == typeof(Version) || conversionType == typeof(TimeSpan) || conversionType == typeof(DateTimeOffset));

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceType">原类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="sourceExpression">数据源表达式。</param>
        /// <returns></returns>
        protected override Expression ToSolve(Type sourceType, Type conversionType, Expression sourceExpression)
            => Expression.Call(conversionType.GetMethod(nameof(Guid.Parse), MapExtensions.StaticFlags, null, new Type[] { sourceType }, null), sourceExpression);
    }
}

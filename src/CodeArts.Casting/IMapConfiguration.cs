using System;
using System.Linq.Expressions;

namespace CodeArts.Casting
{
    /// <summary>
    /// 映射。
    /// </summary>
    public interface IMapConfiguration : IProfileConfiguration
    {
        /// <summary>
        /// 映射。
        /// </summary>
        /// <typeparam name="TResult">结果类型。</typeparam>
        /// <param name="source">源对象。</param>
        /// <param name="def">默认值。</param>
        /// <returns></returns>
        TResult Map<TResult>(object source, TResult def = default);

        /// <summary>
        /// 映射。
        /// </summary>
        /// <param name="sourceExpression">源数据。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="def">目标默认值。</param>
        /// <returns></returns>
        Expression Map(Expression sourceExpression, Type conversionType, Expression def = null);
    }
}

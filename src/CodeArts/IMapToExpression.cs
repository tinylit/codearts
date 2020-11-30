using System;

namespace CodeArts
{
    /// <summary>
    /// 映射表达式。
    /// </summary>
    public interface IMapToExpression : IProfileExpression, IProfileConfiguration, IProfile
    {
        /// <summary>
        /// 拷贝。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <returns></returns>
        T Map<T>(object source, T def = default);

        /// <summary>
        /// 拷贝。
        /// </summary>
        /// <param name="source">数据源。</param>
        /// <param name="conversionType">类型。</param>
        /// <returns></returns>
        object Map(object source, Type conversionType);
    }
}

using System;

namespace CodeArts.Casting
{
    /// <summary>
    /// 映射表达式。
    /// </summary>
    public interface IMapToExpression : IProfileExpression, IProfileConfiguration, IProfile
    {
        /// <summary>
        /// 拷贝。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <returns></returns>
        T Map<T>(object source, T def = default);

        /// <summary>
        /// 拷贝（异常上抛）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <returns></returns>
        T ThrowsMap<T>(object source);

        /// <summary>
        /// 拷贝。
        /// </summary>
        /// <param name="source">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        object Map(object source, Type conversionType);

        /// <summary>
        /// 拷贝（异常上抛）。
        /// </summary>
        /// <param name="source">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        object ThrowsMap(object source, Type conversionType);
    }
}

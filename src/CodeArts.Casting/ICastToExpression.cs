using System;

namespace CodeArts.Casting
{
    /// <summary>
    /// 转换配置。
    /// </summary>
    public interface ICastToExpression : IProfileExpression, IProfile
    {
        /// <summary>
        /// 转换。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <returns></returns>
        T Cast<T>(object source, T def = default);

        /// <summary>
        /// 转换（异常上抛）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <returns></returns>
        T ThrowsCast<T>(object source);

        /// <summary>
        /// 转换。
        /// </summary>
        /// <param name="source">数据源。</param>
        /// <param name="conversionType">类型。</param>
        /// <returns></returns>
        object Cast(object source, Type conversionType);

        /// <summary>
        /// 转换（异常上抛）。
        /// </summary>
        /// <param name="source">源数据。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        object ThrowsCast(object source, Type conversionType);
    }
}

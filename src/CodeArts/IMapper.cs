using System;

namespace CodeArts
{
    /// <summary>
    /// 映射器。
    /// </summary>
    public interface IMapper
    {
        /// <summary>
        /// 对象转换。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        T Cast<T>(object obj, T def = default);

        /// <summary>
        /// 转换（异常上抛）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <returns></returns>
        T ThrowsCast<T>(object source);

        /// <summary> 
        /// 对象转换。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        object Cast(object obj, Type conversionType);

        /// <summary>
        /// 转换（异常上抛）。
        /// </summary>
        /// <param name="source">源数据。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        object ThrowsCast(object source, Type conversionType);

        /// <summary>
        /// 对象克隆。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        T Copy<T>(T obj, T def = default);

        /// <summary>
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        T Map<T>(object obj, T def = default);

        /// <summary>
        /// 拷贝（异常上抛）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <returns></returns>
        T ThrowsMap<T>(object source);

        /// <summary> 
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        object Map(object obj, Type conversionType);

        /// <summary>
        /// 拷贝（异常上抛）。
        /// </summary>
        /// <param name="source">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        object ThrowsMap(object source, Type conversionType);
    }
}

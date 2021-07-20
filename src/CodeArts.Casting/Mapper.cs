using CodeArts.Casting;
using System;

namespace CodeArts
{
    /// <summary>
    /// 映射。
    /// </summary>
    public static class Mapper
    {
        private static readonly IMapper mapper;

        /// <summary>
        /// inheritdoc
        /// </summary>
        static Mapper() => mapper = RuntimeServPools.Singleton<IMapper, CastingMapper>();

        /// <summary>
        /// 对象转换。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public static T Cast<T>(object obj, T def = default) => mapper.Cast(obj, def);

        /// <summary>
        /// 转换（异常上抛）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <returns></returns>
        public static T ThrowsCast<T>(object source) => mapper.ThrowsCast<T>(source);

        /// <summary> 
        /// 对象转换。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public static object Cast(object obj, Type conversionType) => mapper.Cast(obj, conversionType);

        /// <summary>
        /// 转换（异常上抛）。
        /// </summary>
        /// <param name="source">源数据。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public static object ThrowsCast(object source, Type conversionType) => mapper.ThrowsCast(source, conversionType);

        /// <summary>
        /// 对象克隆。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public static T Copy<T>(T obj, T def = default) => mapper.Copy(obj, def);

        /// <summary>
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public static T Map<T>(object obj, T def = default) => mapper.Map(obj, def);

        /// <summary>
        /// 拷贝（异常上抛）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <returns></returns>
        public static T ThrowsMap<T>(object source) => mapper.ThrowsMap<T>(source);

        /// <summary> 
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public static object Map(object obj, Type conversionType) => mapper.Map(obj, conversionType);

        /// <summary>
        /// 拷贝（异常上抛）。
        /// </summary>
        /// <param name="source">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public static object ThrowsMap(object source, Type conversionType) => mapper.ThrowsMap(source, conversionType);
    }
}

using CodeArts.Casting.Implements;
using System;

namespace CodeArts.Casting
{
    /// <summary>
    /// 转换映射。
    /// </summary>
    public class CastingMapper : IMapper
    {
        private readonly ICastToExpression castToExpression;
        private readonly ICopyToExpression copyToExpression;
        private readonly IMapToExpression mapToExpression;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public CastingMapper()
        {
            castToExpression = RuntimeServPools.Singleton<ICastToExpression, CastToExpression>();
            copyToExpression = RuntimeServPools.Singleton<ICopyToExpression, CopyToExpression>();
            mapToExpression = RuntimeServPools.Singleton<IMapToExpression, MapToExpression>();
        }

        /// <summary>
        /// 对象转换。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public T Cast<T>(object obj, T def = default) => castToExpression.Cast(obj, def);

        /// <summary>
        /// 转换（异常上抛）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <returns></returns>
        public T ThrowsCast<T>(object source) => castToExpression.ThrowsCast<T>(source);

        /// <summary> 
        /// 对象转换。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public object Cast(object obj, Type conversionType) => castToExpression.Cast(obj, conversionType);

        /// <summary>
        /// 转换（异常上抛）。
        /// </summary>
        /// <param name="source">源数据。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public object ThrowsCast(object source, Type conversionType) => castToExpression.ThrowsCast(source, conversionType);

        /// <summary>
        /// 对象克隆。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public T Copy<T>(T obj, T def = default) => copyToExpression.Copy(obj, def);

        /// <summary>
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public T Map<T>(object obj, T def = default) => mapToExpression.Map(obj, def);

        /// <summary>
        /// 拷贝（异常上抛）。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <returns></returns>
        public T ThrowsMap<T>(object source) => mapToExpression.ThrowsMap<T>(source);

        /// <summary> 
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public object Map(object obj, Type conversionType) => mapToExpression.Map(obj, conversionType);

        /// <summary>
        /// 拷贝（异常上抛）。
        /// </summary>
        /// <param name="source">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public object ThrowsMap(object source, Type conversionType) => mapToExpression.ThrowsMap(source, conversionType);
    }
}

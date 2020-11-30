using System;

namespace CodeArts
{
    /// <summary>
    /// 映射。
    /// </summary>
    public static class Mapper
    {
        /// <summary>
        /// 转换接口。
        /// </summary>
        private static ICastToExpression instanceCast => RuntimeServPools.Singleton<ICastToExpression>();

        /// <summary>
        /// 对象转换。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public static T Cast<T>(object obj, T def = default) => instanceCast.Cast(obj, def);

        /// <summary> 
        /// 对象转换。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public static object Cast(object obj, Type conversionType) => instanceCast.Cast(obj, conversionType);

        /// <summary>
        /// 复制接口。
        /// </summary>
        private static ICopyToExpression instanceCopy => RuntimeServPools.Singleton<ICopyToExpression>();

        /// <summary>
        /// 对象克隆。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public static T Copy<T>(T obj, T def = default) => instanceCopy.Copy(obj, def);

        /// <summary>
        /// 映射接口。
        /// </summary>
        private static IMapToExpression instanceMap => RuntimeServPools.Singleton<IMapToExpression>();

        /// <summary>
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="def">默认值。</param>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <returns></returns>
        public static T Map<T>(object obj, T def = default) => instanceMap.Map(obj, def);

        /// <summary> 
        /// 对象映射。
        /// </summary>
        /// <param name="obj">数据源。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public static object Map(object obj, Type conversionType) => instanceMap.Map(obj, conversionType);
    }
}

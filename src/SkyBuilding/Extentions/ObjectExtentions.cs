using SkyBuilding;
using SkyBuilding.Implements;
using System.Collections;
using System.Collections.Generic;

namespace System
{
    /// <summary>
    /// 对象扩展
    /// </summary>
    public static class ObjectExtentions
    {
        /// <summary>
        /// 对象转换
        /// </summary>
        /// <param name="obj">数据源</param>
        /// <param name="def">默认值</param>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns></returns>
        public static T CastTo<T>(this object obj, T def = default) =>
            RuntimeServicePools.Singleton<ICastToExpression, CastToExpression>()
            .CastTo(obj, def);

        /// <summary> 
        /// 对象转换 
        /// </summary>
        /// <param name="obj">数据源</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        public static object CastTo(this object obj, Type conversionType) =>
            RuntimeServicePools.Singleton<ICastToExpression, CastToExpression>()
            .CastTo(obj, conversionType);

        /// <summary>
        /// 对象克隆
        /// </summary>
        /// <param name="obj">数据源</param>
        /// <param name="def">默认值</param>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns></returns>
        public static T CopyTo<T>(this T obj, T def = default) =>
            RuntimeServicePools.Singleton<ICopyToExpression, MapToExpression>()
            .CopyTo(obj, def);

        /// <summary>
        /// 对象映射
        /// </summary>
        /// <param name="obj">数据源</param>
        /// <param name="def">默认值</param>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns></returns>
        public static T MapTo<T>(this object obj, T def = default) =>
            RuntimeServicePools.Singleton<IMapToExpression, MapToExpression>()
            .MapTo(obj, def);

        /// <summary> 
        /// 对象映射
        /// </summary>
        /// <param name="obj">数据源</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        public static object MapTo(this object obj, Type conversionType) =>
            RuntimeServicePools.Singleton<IMapToExpression, MapToExpression>()
            .MapTo(obj, conversionType);
    }
}

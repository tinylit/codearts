using CodeArts;
using CodeArts.Implements;

namespace System
{
    /// <summary>
    /// 对象扩展
    /// </summary>
    public static class ObjectExtentions
    {
        private static ICastToExpression cast;
        /// <summary>
        /// 转换接口
        /// </summary>
        private static ICastToExpression Cast
        {
            get
            {
                if (cast is null)
                {
                    cast = RuntimeServManager.Singleton<ICastToExpression, CastToExpression>(value => cast = value);
                }

                return cast;
            }
        }

        /// <summary>
        /// 对象转换
        /// </summary>
        /// <param name="obj">数据源</param>
        /// <param name="def">默认值</param>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns></returns>
        public static T CastTo<T>(this object obj, T def = default) => Cast.Cast(obj, def);

        /// <summary> 
        /// 对象转换 
        /// </summary>
        /// <param name="obj">数据源</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        public static object CastTo(this object obj, Type conversionType) => Cast.Cast(obj, conversionType);

        private static ICopyToExpression copy;
        /// <summary>
        /// 复制接口
        /// </summary>
        private static ICopyToExpression Copy
        {
            get
            {
                if (copy is null)
                {
                    copy = RuntimeServManager.Singleton<ICopyToExpression, MapToExpression>(value => copy = value);
                }

                return copy;
            }
        }

        /// <summary>
        /// 对象克隆
        /// </summary>
        /// <param name="obj">数据源</param>
        /// <param name="def">默认值</param>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns></returns>
        public static T CopyTo<T>(this T obj, T def = default) => Copy.Copy(obj, def);

        private static IMapToExpression map;
        /// <summary>
        /// 映射接口
        /// </summary>
        private static IMapToExpression Map
        {
            get
            {
                if (map is null)
                {
                    map = RuntimeServManager.Singleton<IMapToExpression, MapToExpression>(value => map = value);
                }

                return map;
            }
        }

        /// <summary>
        /// 对象映射
        /// </summary>
        /// <param name="obj">数据源</param>
        /// <param name="def">默认值</param>
        /// <typeparam name="T">目标类型</typeparam>
        /// <returns></returns>
        public static T MapTo<T>(this object obj, T def = default) => Map.Map(obj, def);

        /// <summary> 
        /// 对象映射
        /// </summary>
        /// <param name="obj">数据源</param>
        /// <param name="conversionType">目标类型</param>
        /// <returns></returns>
        public static object MapTo(this object obj, Type conversionType) => Map.Map(obj, conversionType);
    }
}

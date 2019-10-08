using System.Collections.Generic;

namespace System
{
    /// <summary>
    /// 类型扩展
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// 判断类型是否为Nullable类型
        /// </summary>
        /// <param name="type"> 要处理的类型 </param>
        /// <returns> 是返回True，不是返回False </returns>
        public static bool IsNullable(this Type type) => type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        /// <summary>
        /// 判断类型是否为KeyValuePair类型
        /// </summary>
        /// <param name="type"> 要处理的类型 </param>
        /// <returns> 是返回True，不是返回False </returns>
        public static bool IsKeyValuePair(this Type type) => type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
    }
}

using System;
using System.Linq;

namespace CodeArts.ORM
{
    /// <summary>
    /// 类型拓展类
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// 是否为boolean类型或boolean可空类型
        /// </summary>
        /// <param name="type">类型入参</param>
        /// <returns></returns>
        public static bool IsBoolean(this Type type) => type == typeof(bool) || type == typeof(bool?);

        /// <summary>
        /// 是否是查询器的派生类。
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static bool IsQueryable(this Type type) => typeof(IQueryable).IsAssignableFrom(type);

        /// <summary>
        /// 是否为声明类型，或type为泛型且泛型参数类型包含声明。
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="declaringType">声明类型</param>
        /// <returns></returns>
        public static bool IsDeclaringType(this Type type, Type declaringType)
        {
            if (type.IsGenericType)
            {
                foreach (var item in type.GetGenericArguments())
                {
                    if (item == declaringType) return true;
                }
                return false;
            }
            return type == declaringType;
        }

        /// <summary>
        /// 查找指定类型
        /// </summary>
        /// <returns></returns>
        public static Type FindGenericType(this Type type, Type definition)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
                    return type;

                if (definition.IsInterface)
                {
                    Type[] interfaces = type.GetInterfaces();
                    foreach (Type type2 in interfaces)
                    {
                        Type type3 = FindGenericType(definition, type2);

                        if (type3 is null) continue;

                        return type3;
                    }
                }

                type = type.BaseType;
            }
            return null;
        }
    }
}

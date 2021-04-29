using System;
using System.Linq;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 类型拓展类。
    /// </summary>
    internal static class TypeExtensions
    {
        static readonly Type Queryable_T_Type = typeof(IQueryable<>);
        static readonly Type Grouping_T1_T2_Type = typeof(IGrouping<,>);

        /// <summary>
        /// 是否为boolean类型或boolean可空类型。
        /// </summary>
        /// <param name="type">类型入参。</param>
        /// <returns></returns>
        public static bool IsBoolean(this Type type) => type.IsValueType && (type == typeof(bool) || type == typeof(bool?));

        /// <summary>
        /// 是否是查询器的派生类。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>
        public static bool IsQueryable(this Type type) => Types.IQueryable.IsAssignableFrom(type);

        /// <summary>
        /// 是否是<see cref="IQueryable{T}"/>并且<seealso cref="IGrouping{TKey, TElement}"/>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>
        public static bool IsGroupingQueryable(this Type type)
        {
            if (type is null)
            {
                return false;
            }

            while (type.IsQueryable())
            {
                if (!IsGenericType(type, out Type[] typeArguments))
                {
                    goto label_continue;
                }

                if (typeArguments.Length > 1)
                {
                    goto label_continue;
                }

                if (IsGrouping(typeArguments[0]))
                {
                    return true;
                }

                label_continue:
                {
                    type = type.BaseType;
                }
            }

            return false;
        }

        private static bool IsGenericType(Type typeSelf, out Type[] type2Arguments)
        {
            while (typeSelf != null && typeSelf != typeof(object))
            {
                if (typeSelf.IsInterface && typeSelf.IsGenericType && typeSelf.GetGenericTypeDefinition() == Queryable_T_Type)
                {
                    type2Arguments = typeSelf.GetGenericArguments();

                    return true;
                }

                Type[] interfaces = typeSelf.GetInterfaces();

                foreach (Type type2 in interfaces)
                {
                    if (IsGenericType(type2, out type2Arguments))
                    {
                        return true;
                    }
                }

                typeSelf = typeSelf.BaseType;
            }

            type2Arguments = Type.EmptyTypes;

            return false;
        }

        /// <summary>
        /// <see cref="IGrouping{TKey, TElement}"/>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>
        public static bool IsGrouping(this Type type) => type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == Grouping_T1_T2_Type;

        /// <summary>
        /// 查找指定类型。
        /// </summary>
        /// <returns></returns>
        public static Type FindGenericType(this Type type, Type definition)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
                {
                    return type;
                }

                if (definition.IsInterface)
                {
                    Type[] interfaces = type.GetInterfaces();

                    foreach (Type type2 in interfaces)
                    {
                        Type type3 = FindGenericType(type2, definition);

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

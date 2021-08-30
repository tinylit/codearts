using System;
using System.Collections.Generic;

namespace CodeArts.Emit
{
    /// <summary>
    /// 类型工具。
    /// </summary>
    internal static class TypeExtensions
    {
        internal static Type[] GetAllInterfaces(this Type[] types)
        {
            if (types is null || types.Length == 0)
            {
                return Type.EmptyTypes;
            }

            var interfaces = new HashSet<Type>();
            for (var index = 0; index < types.Length; index++)
            {
                var type = types[index];
                if (type is null)
                {
                    continue;
                }

                if (type.IsInterface)
                {
                    if (!interfaces.Add(type))
                    {
                        continue;
                    }
                }

                var innerInterfaces = type.GetInterfaces();

                for (var i = 0; i < innerInterfaces.Length; i++)
                {
                    interfaces.Add(innerInterfaces[i]);
                }
            }

            return Sort(interfaces);
        }

        internal static Type[] GetAllInterfaces(this Type type)  // NOTE: also used by Windsor
        {
            return GetAllInterfaces(new Type[1] { type });
        }

        private static Type[] Sort(ICollection<Type> types)
        {
            var array = new Type[types.Count];
            types.CopyTo(array, 0);
            //NOTE: is there a better, stable way to sort Types. We will need to revise this once we allow open generics
            Array.Sort(array, TypeNameComparer.Instance);
            //                ^^^^^^^^^^^^^^^^^^^^^^^^^
            // Using a `IComparer<T>` object instead of a `Comparison<T>` delegate prevents
            // an unnecessary level of indirection inside the framework (as the latter get
            // wrapped as `IComparer<T>` objects).
            return array;
        }
        private sealed class TypeNameComparer : IComparer<Type>
        {
            public static readonly TypeNameComparer Instance = new TypeNameComparer();

            public int Compare(Type x, Type y)
            {
                // Comparing by `type.AssemblyQualifiedName` would give the same result,
                // but it performs a hidden concatenation (and therefore string allocation)
                // of `type.FullName` and `type.Assembly.FullName`. We can avoid this
                // overhead by comparing the two properties separately.
                int result = string.CompareOrdinal(x.FullName, y.FullName);
                return result == 0
                    ? string.CompareOrdinal(x.Assembly.FullName, y.Assembly.FullName)
                    : result;
            }
        }
    }
}

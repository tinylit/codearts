using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeArts
{
    class MethodInfoEqualityComparer : IEqualityComparer<MethodInfo>
    {
        private MethodInfoEqualityComparer() { }

        public static MethodInfoEqualityComparer Instance = new MethodInfoEqualityComparer();
        public bool EqualGenericParameters(MethodInfo x, MethodInfo y)
        {
            if (x.IsGenericMethod != y.IsGenericMethod)
            {
                return false;
            }

            if (x.IsGenericMethod)
            {
                var xArgs = x.GetGenericArguments();
                var yArgs = y.GetGenericArguments();

                if (xArgs.Length != yArgs.Length)
                {
                    return false;
                }

                for (var i = 0; i < xArgs.Length; ++i)
                {
                    if (xArgs[i].IsGenericParameter != yArgs[i].IsGenericParameter)
                    {
                        return false;
                    }

                    if (!xArgs[i].IsGenericParameter && !xArgs[i].Equals(yArgs[i]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool EqualParameters(MethodInfo x, MethodInfo y)
        {
            var xArgs = x.GetParameters();
            var yArgs = y.GetParameters();

            if (xArgs.Length != yArgs.Length)
            {
                return false;
            }

            for (var i = 0; i < xArgs.Length; ++i)
            {
                if (!EqualSignatureTypes(xArgs[i].ParameterType, yArgs[i].ParameterType))
                {
                    return false;
                }
            }

            return true;
        }

        public bool EqualSignatureTypes(Type x, Type y)
        {
            if (x.IsGenericParameter != y.IsGenericParameter)
            {
                return false;
            }
            else if (x.IsGenericType != y.IsGenericType)
            {
                return false;
            }

            if (x.IsGenericParameter)
            {
                if (x.GenericParameterPosition != y.GenericParameterPosition)
                {
                    return false;
                }
            }
            else if (x.IsGenericType)
            {
                var xGenericTypeDef = x.GetGenericTypeDefinition();
                var yGenericTypeDef = y.GetGenericTypeDefinition();

                if (xGenericTypeDef != yGenericTypeDef)
                {
                    return false;
                }

                var xArgs = x.GetGenericArguments();
                var yArgs = y.GetGenericArguments();

                if (xArgs.Length != yArgs.Length)
                {
                    return false;
                }

                for (var i = 0; i < xArgs.Length; ++i)
                {
                    if (!EqualSignatureTypes(xArgs[i], yArgs[i])) return false;
                }
            }
            else
            {
                if (!x.Equals(y))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Equals(MethodInfo x, MethodInfo y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return EqualNames(x, y) &&
                   EqualGenericParameters(x, y) &&
                   EqualSignatureTypes(x.ReturnType, y.ReturnType) &&
                   EqualParameters(x, y);
        }

        public int GetHashCode(MethodInfo obj)
        {
            return obj.Name.GetHashCode() ^ obj.GetParameters().Length; // everything else would be too cumbersome
        }

        private bool EqualNames(MethodInfo x, MethodInfo y)
        {
            return x.Name == y.Name;
        }
    }
}

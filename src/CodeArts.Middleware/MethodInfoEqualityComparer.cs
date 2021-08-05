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

        public bool EqualParameters(MethodInfo x, MethodInfo y, Type[] genericArguments)
        {
            var xArgs = x.GetParameters();
            var yArgs = y.GetParameters();

            if (xArgs.Length != yArgs.Length)
            {
                return false;
            }

            for (var i = 0; i < xArgs.Length; ++i)
            {
                if (!EqualSignatureTypes(xArgs[i].ParameterType, yArgs[i].ParameterType, genericArguments))
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

        public bool EqualSignatureTypes(Type x, Type y, Type[] genericArguments)
        {
            if (x.IsGenericParameter)
            {
                if (genericArguments.Length > x.GenericParameterPosition)
                {
                    return EqualSignatureTypes(genericArguments[x.GenericParameterPosition], y, genericArguments);
                }

                return false;
            }

            if (x.IsGenericType != y.IsGenericType)
            {
                return false;
            }

            if (x.IsGenericType)
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
                    if (!EqualSignatureTypes(xArgs[i], yArgs[i], genericArguments))
                        return false;
                }
            }
            else if (!x.Equals(y))
            {
                return false;
            }

            return true;
        }

        public bool EqualGenericDeclaringType(MethodInfo x, MethodInfo y)
        {
            var declaringType = y.DeclaringType;

            var typeDefinition = x.DeclaringType.GetGenericTypeDefinition();

            do
            {
                if (declaringType.IsGenericType && declaringType.GetGenericTypeDefinition() == typeDefinition)
                {
                    var genericArguments = declaringType.GetGenericArguments();

                    if (genericArguments.Any(g => g.IsGenericParameter))
                    {
                        break;
                    }

                    return EqualSignatureTypes(x.ReturnType, y.ReturnType, genericArguments) && EqualParameters(x, y, genericArguments);
                }

            } while ((declaringType = declaringType.BaseType) != null);

            return false;
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

            if (!EqualNames(x, y) || !EqualGenericParameters(x, y))
            {
                return false;
            }

            if (EqualSignatureTypes(x.ReturnType, y.ReturnType) &&
                   EqualParameters(x, y))
            {
                return true;
            }

            if (x.DeclaringType.IsGenericType && EqualGenericDeclaringType(x, y))
            {
                return true;
            }

            if (y.DeclaringType.IsGenericType && EqualGenericDeclaringType(y, x))
            {
                return true;
            }

            return false;
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

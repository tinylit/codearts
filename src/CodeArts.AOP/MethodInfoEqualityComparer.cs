using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeArts
{
    class MethodInfoEqualityComparer : IEqualityComparer<MethodInfo>
    {
        private MethodInfoEqualityComparer() { }

        public static MethodInfoEqualityComparer Instance = new MethodInfoEqualityComparer();
        public bool Equals(MethodInfo x, MethodInfo y)
        {
            if (x is null)
            {
                return y is null;
            }

            if (y is null)
            {
                return false;
            }

            if (x == y)
            {
                return true;
            }

            if (x.IsGenericMethod ^ y.IsGenericMethod)
            {
                return false;
            }

            if (!string.Equals(x.Name, y.Name))
            {
                return false;
            }

            if (x.ReturnType != y.ReturnType)
            {
                if (x.ReturnType.IsGenericType && y.ReturnType.IsGenericType)
                {
                    if (x.ReturnType.GetGenericTypeDefinition() != y.ReturnType.GetGenericTypeDefinition())
                    {
                        return false;
                    }
                }
                else if (!x.ReturnType.IsGenericParameter || !y.ReturnType.IsGenericParameter)
                {
                    return false;
                }
            }

            var xParameters = x.GetParameters();
            var yParameters = y.GetParameters();

            if (xParameters.Length != yParameters.Length)
            {
                return false;
            }

            return xParameters
                .Zip(yParameters, (x1, y1) => x1.ParameterType == y1.ParameterType || x1.ParameterType.IsGenericParameter && y1.ParameterType.IsGenericParameter)
                .All(isEquals => isEquals);
        }

        public int GetHashCode(MethodInfo obj) => obj?.Name.GetHashCode() ?? 0;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace CodeArts.Emit
{
    class DynamicMethod : MethodInfo
    {
        private readonly Type declaringType;
        private readonly Type returnType;
        private readonly MethodInfo methodInfoOriginal;
        private readonly Type[] declaringTypeParameters;

        public DynamicMethod(Type declaringType, Type returnType, MethodInfo methodInfoOriginal, Type[] declaringTypeParameters)
        {
            this.declaringType = declaringType;
            this.returnType = returnType;
            this.methodInfoOriginal = methodInfoOriginal;
            this.declaringTypeParameters = declaringTypeParameters;
        }

        public Type DynamicDeclaringType => declaringType;
        public Type DynamicReturnType => returnType;

        public override string Name => methodInfoOriginal.Name;

        public override Type DeclaringType => methodInfoOriginal.DeclaringType;

        public override Type ReflectedType => methodInfoOriginal.ReflectedType;
        public override ParameterInfo ReturnParameter => methodInfoOriginal.ReturnParameter;
        public override Type ReturnType => methodInfoOriginal.ReturnType;
        public override MethodInfo GetBaseDefinition() => methodInfoOriginal;
        public override ParameterInfo[] GetParameters() => methodInfoOriginal.GetParameters();

#if NET45_OR_GREATER
        public override Delegate CreateDelegate(Type delegateType) => methodInfoOriginal.CreateDelegate(delegateType);

        public override Delegate CreateDelegate(Type delegateType, object target) => methodInfoOriginal.CreateDelegate(delegateType, target);
#endif

        public override Type[] GetGenericArguments() => methodInfoOriginal.GetGenericArguments();

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => methodInfoOriginal.ReturnTypeCustomAttributes;

        public override RuntimeMethodHandle MethodHandle => methodInfoOriginal.MethodHandle;

        public override MethodAttributes Attributes => methodInfoOriginal.Attributes;
        public override CallingConventions CallingConvention => methodInfoOriginal.CallingConvention;

        public override bool ContainsGenericParameters => methodInfoOriginal.ContainsGenericParameters;

#if NET45_OR_GREATER
        public override IEnumerable<CustomAttributeData> CustomAttributes => methodInfoOriginal.CustomAttributes;
#endif

        public override bool IsGenericMethod => methodInfoOriginal.IsGenericMethod;
        public override bool IsGenericMethodDefinition => false;
        public override bool IsSecurityCritical => methodInfoOriginal.IsSecurityCritical;
        public override bool IsSecuritySafeCritical => methodInfoOriginal.IsSecuritySafeCritical;
        public override bool IsSecurityTransparent => methodInfoOriginal.IsSecurityTransparent;
        public override MemberTypes MemberType => methodInfoOriginal.MemberType;
        public override int MetadataToken => methodInfoOriginal.MetadataToken;
#if NET45_OR_GREATER
        public override MethodImplAttributes MethodImplementationFlags => methodInfoOriginal.MethodImplementationFlags;
#endif
        public override Module Module => methodInfoOriginal.Module;

        public override object[] GetCustomAttributes(bool inherit) => methodInfoOriginal.GetCustomAttributes(inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => methodInfoOriginal.GetCustomAttributes(attributeType, inherit);

        public override MethodImplAttributes GetMethodImplementationFlags() => methodInfoOriginal.GetMethodImplementationFlags();

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => methodInfoOriginal.Invoke(obj, invokeAttr, binder, parameters, culture);

        public override bool IsDefined(Type attributeType, bool inherit) => methodInfoOriginal.IsDefined(attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() => methodInfoOriginal.GetCustomAttributesData();

        public override MethodInfo GetGenericMethodDefinition()
        {
            var methodDefinition = methodInfoOriginal.GetGenericMethodDefinition();

            return new DynamicMethod(declaringType, returnType, methodDefinition, declaringTypeParameters);
        }

        public override bool Equals(object obj) => methodInfoOriginal.Equals(obj);

        public override int GetHashCode() => methodInfoOriginal.GetHashCode();

        public override MethodBody GetMethodBody() => methodInfoOriginal.GetMethodBody();

        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            var methodInfo = methodInfoOriginal.MakeGenericMethod(typeArguments);

            return new DynamicMethod(declaringType, MakeGenericParameter(methodInfo.ReturnType, typeArguments, declaringTypeParameters), methodInfo, declaringTypeParameters);
        }

        public override string ToString() => methodInfoOriginal.ToString();

        private static Type MakeGenericParameter(Type type, Type[] genericArguments, Type[] typeParameterBuilders)
        {
            if (type.IsGenericParameter)
            {
                if (Array.IndexOf(genericArguments, type) > -1)
                {
                    return type;
                }

                return typeParameterBuilders[type.GenericParameterPosition];
            }

            if (type.IsGenericType)
            {
                var genericArguments2 = type.GetGenericArguments();

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    genericArguments2[i] = MakeGenericParameter(genericArguments[i], genericArguments, typeParameterBuilders);
                }

                return type.GetGenericTypeDefinition().MakeGenericType(genericArguments);
            }

            if (type.IsArray)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), genericArguments, typeParameterBuilders);
                int rank = type.GetArrayRank();

                return rank == 1
                    ? elementType.MakeArrayType()
                    : elementType.MakeArrayType(rank);
            }

            if (type.IsByRef)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), genericArguments, typeParameterBuilders);

                return elementType.MakeByRefType();
            }

            return type;
        }
    }
}

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
        private readonly MethodInfo methodInfoDeclaration;
        private readonly bool hasDeclaringTypes;

        public DynamicMethod(MethodInfo methodInfoOriginal, MethodInfo methodInfoDeclaration, Type declaringType, Type returnType, Type[] declaringTypeParameters, bool hasDeclaringTypes)
        {
            this.methodInfoOriginal = methodInfoOriginal;
            this.methodInfoDeclaration = methodInfoDeclaration;
            this.declaringType = declaringType;
            this.returnType = returnType;
            this.declaringTypeParameters = declaringTypeParameters;
            this.hasDeclaringTypes = hasDeclaringTypes;
        }

        public DynamicMethod(MethodInfo methodInfoOriginal, Type declaringType, Type returnType, Type[] declaringTypeParameters, bool hasDeclaringTypes) : this(methodInfoOriginal, methodInfoOriginal, declaringType, returnType, declaringTypeParameters, hasDeclaringTypes)
        {

        }

        public MethodInfo RuntimeMethod => methodInfoDeclaration;

        public override string Name => methodInfoOriginal.Name;

        public override Type DeclaringType => declaringType;

        public override Type ReflectedType => methodInfoOriginal.ReflectedType;

        public override ParameterInfo ReturnParameter => methodInfoOriginal.ReturnParameter;

        public override Type ReturnType => returnType;

        public override MethodInfo GetBaseDefinition() => methodInfoOriginal;

        public override ParameterInfo[] GetParameters() => methodInfoOriginal.GetParameters();

#if NET45_OR_GREATER
        public override Delegate CreateDelegate(Type delegateType) => methodInfoDeclaration.CreateDelegate(delegateType);

        public override Delegate CreateDelegate(Type delegateType, object target) => methodInfoDeclaration.CreateDelegate(delegateType, target);
#endif

        public override Type[] GetGenericArguments() => methodInfoDeclaration.GetGenericArguments();

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

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => methodInfoDeclaration.Invoke(obj, invokeAttr, binder, parameters, culture);

        public override bool IsDefined(Type attributeType, bool inherit) => methodInfoOriginal.IsDefined(attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() => methodInfoOriginal.GetCustomAttributesData();

        public override MethodInfo GetGenericMethodDefinition()
        {
            var methodInfoDeclaration = methodInfoOriginal.GetGenericMethodDefinition();

            return new DynamicMethod(methodInfoOriginal, methodInfoDeclaration, declaringType, returnType, declaringTypeParameters, hasDeclaringTypes);
        }

        public override bool Equals(object obj) => methodInfoOriginal.Equals(obj);

        public override int GetHashCode() => methodInfoOriginal.GetHashCode();

        public override MethodBody GetMethodBody() => methodInfoOriginal.GetMethodBody();

        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            var methodInfoDeclaration = methodInfoOriginal.IsGenericMethodDefinition
                ? methodInfoOriginal.MakeGenericMethod(typeArguments)
                : methodInfoOriginal.GetGenericMethodDefinition().MakeGenericMethod(typeArguments);

            return hasDeclaringTypes
                ? new DynamicMethod(methodInfoOriginal, methodInfoDeclaration, declaringType, MakeGenericParameter(methodInfoDeclaration.ReturnType, typeArguments, declaringTypeParameters), declaringTypeParameters, hasDeclaringTypes)
                : new DynamicMethod(methodInfoOriginal, methodInfoDeclaration, declaringType, methodInfoDeclaration.ReturnType, declaringTypeParameters, hasDeclaringTypes);
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

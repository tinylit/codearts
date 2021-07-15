using CodeArts.Emit.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security;

namespace CodeArts.Emit
{
    /// <summary>
    /// 抽象类。
    /// </summary>
    public abstract class AbstractTypeEmitter
    {
        private readonly TypeBuilder builder;
        private readonly INamingScope namingScope;
        private readonly List<MethodEmitter> methods = new List<MethodEmitter>();
        private readonly List<AbstractTypeEmitter> abstracts = new List<AbstractTypeEmitter>();
        private readonly List<ConstructorEmitter> constructors = new List<ConstructorEmitter>();
        private readonly Dictionary<string, FieldEmitter> fields = new Dictionary<string, FieldEmitter>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PropertyEmitter> properties = new Dictionary<string, PropertyEmitter>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        public class TypeInitializerEmitter : BlockAst
        {
            internal TypeInitializerEmitter() : base(typeof(void))
            {
            }

            /// <summary>
            /// 发行。
            /// </summary>
            internal void Emit(ConstructorBuilder builder)
            {
#if NET40
                var attributes = builder.GetMethodImplementationFlags();
#else
                var attributes = builder.MethodImplementationFlags;
#endif

                if ((attributes & MethodImplAttributes.Runtime) != MethodImplAttributes.IL)
                {
                    return;
                }

                var ilg = builder.GetILGenerator();

                base.Load(ilg);

                if (IsLastReturn)
                {
                    return;
                }

                if (!IsEmpty && RuntimeType == typeof(void))
                {
                    ilg.Emit(OpCodes.Nop);
                }

                ilg.Emit(OpCodes.Ret);
            }
        }

        private class RuntimeMethodInfo : MethodInfo
        {
            private readonly MethodInfo methodInfoOriginal;
            private readonly MethodInfo methodInfoDeclaration;

            public RuntimeMethodInfo(MethodInfo methodInfoOriginal, MethodInfo methodInfoDeclaration)
            {
                this.methodInfoOriginal = methodInfoOriginal;
                this.methodInfoDeclaration = methodInfoDeclaration;
            }

            public override string Name => methodInfoOriginal.Name;

            public override Type DeclaringType => methodInfoDeclaration.DeclaringType;

            public override Type ReflectedType => methodInfoDeclaration.ReflectedType;
            public override ParameterInfo ReturnParameter => methodInfoDeclaration.ReturnParameter;
            public override Type ReturnType => methodInfoDeclaration.ReturnType;
            public override MethodInfo GetBaseDefinition() => methodInfoDeclaration.GetBaseDefinition();
            public override ParameterInfo[] GetParameters() => methodInfoDeclaration.GetParameters();

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

            public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => methodInfoOriginal.Invoke(obj, invokeAttr, binder, parameters, culture);

            public override bool IsDefined(Type attributeType, bool inherit) => methodInfoOriginal.IsDefined(attributeType, inherit);

            public override IList<CustomAttributeData> GetCustomAttributesData() => methodInfoOriginal.GetCustomAttributesData();


            public override MethodInfo GetGenericMethodDefinition() => methodInfoOriginal.GetGenericMethodDefinition();

            public override bool Equals(object obj) => methodInfoOriginal.Equals(obj);

            public override int GetHashCode() => methodInfoOriginal.GetHashCode();

            public override MethodBody GetMethodBody() => methodInfoOriginal.GetMethodBody();

            public override MethodInfo MakeGenericMethod(params Type[] typeArguments) => methodInfoOriginal.MakeGenericMethod(typeArguments);

            public override string ToString() => methodInfoOriginal.ToString();
        }

        /// <summary>
        /// 方法。
        /// </summary>
        private sealed class MethodOverrideEmitter : MethodEmitter
        {
            private readonly MethodBuilder methodBuilder;
            private readonly MethodInfo methodInfoDeclaration;

            /// <summary>
            /// 重写方法。
            /// </summary>
            /// <param name="methodBuilder">方法构造器。</param>
            /// <param name="methodInfoDeclaration">被重写的方法。</param>
            public MethodOverrideEmitter(MethodBuilder methodBuilder, MethodInfo methodInfoDeclaration) : base(methodBuilder.Name, methodBuilder.Attributes, methodBuilder.ReturnType)
            {
                if (methodBuilder is null)
                {
                    throw new ArgumentNullException(nameof(methodBuilder));
                }

                if (methodInfoDeclaration is null)
                {
                    throw new ArgumentNullException(nameof(methodInfoDeclaration));
                }

                this.methodBuilder = methodBuilder;

                this.methodInfoDeclaration = methodInfoDeclaration;
            }

            /// <summary>
            /// 是否为泛型方法。
            /// </summary>
            public override bool IsGenericMethod => methodInfoDeclaration.IsGenericMethod;

            /// <summary>
            /// 泛型参数。
            /// </summary>
            /// <returns></returns>
            public override Type[] GetGenericArguments() => methodBuilder.GetGenericArguments();

            /// <summary>
            /// 声明的方法（被重写的方法。）
            /// </summary>
            public MethodInfo MethodInfoDeclaration => methodInfoDeclaration;

            public ParameterEmitter DefineParameter(ParameterInfo parameterInfo, bool skipValid)
            {
                var parameter = skipValid
                    ? base.DefineParameter(parameterInfo.ParameterType, parameterInfo.Attributes, parameterInfo.Name)
                    : base.DefineParameter(parameterInfo);

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
                if (parameterInfo.HasDefaultValue)
#else
                if (parameterInfo.IsOptional)
#endif
                {
                    parameter.SetConstant(parameterInfo.DefaultValue);
                }

                foreach (var customAttribute in parameterInfo.GetCustomAttributesData())
                {
                    parameter.SetCustomAttribute(customAttribute);
                }

                return parameter;
            }

            public override ParameterEmitter DefineParameter(ParameterInfo parameterInfo) => DefineParameter(parameterInfo, false);

            public override ParameterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string name)
            {
                throw new AstException("重新方法不允许自定义参数!");
            }

            /// <summary>
            /// 发行。
            /// </summary>
            /// <param name="builder">构造器。</param>
            public override void Emit(TypeBuilder builder)
            {
                if (builder != methodBuilder.DeclaringType)
                {
                    throw new ArgumentException("方法声明类型和类型构造器不一致!", nameof(builder));
                }

                Emit(methodBuilder);

                if (methodInfoDeclaration.DeclaringType.IsInterface)
                {
                    builder.DefineMethodOverride(methodBuilder, methodInfoDeclaration);
                }
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="builder">类型构造器。</param>
        /// <param name="namingScope">命名。</param>
        protected AbstractTypeEmitter(TypeBuilder builder, INamingScope namingScope)
        {
            this.builder = builder ?? throw new ArgumentNullException(nameof(builder));
            this.namingScope = namingScope ?? throw new ArgumentNullException(nameof(namingScope));
        }

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        protected AbstractTypeEmitter(AbstractTypeEmitter typeEmitter, string name) : this(typeEmitter, name, TypeAttributes.NotPublic)
        {
        }

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        /// <param name="attributes">匿名函数类型。</param>
        protected AbstractTypeEmitter(AbstractTypeEmitter typeEmitter, string name, TypeAttributes attributes) : this(typeEmitter, name, attributes, typeof(object))
        {
        }

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        /// <param name="attributes">匿名函数类型。</param>
        /// <param name="baseType">匿名函数基类。</param>
        protected AbstractTypeEmitter(AbstractTypeEmitter typeEmitter, string name, TypeAttributes attributes, Type baseType) : this(typeEmitter, name, attributes, baseType, Type.EmptyTypes)
        {
        }

        /// <summary>
        /// 创建匿名类型的构造函数。
        /// </summary>
        /// <param name="typeEmitter">匿名类型的所属类型。</param>
        /// <param name="name">匿名类型名称。</param>
        /// <param name="attributes">匿名函数类型。</param>
        /// <param name="baseType">匿名函数基类。</param>
        /// <param name="interfaces">匿名函数实现接口。</param>
        protected AbstractTypeEmitter(AbstractTypeEmitter typeEmitter, string name, TypeAttributes attributes, Type baseType, Type[] interfaces) : this(DefineTypeBuilder(typeEmitter, name, attributes, baseType, interfaces), typeEmitter.namingScope.BeginScope())
        {
            typeEmitter.abstracts.Add(this);
        }

        private static TypeBuilder DefineTypeBuilder(AbstractTypeEmitter typeEmitter, string name, TypeAttributes attributes, Type baseType, Type[] interfaces)
        {
            if (typeEmitter is null)
            {
                throw new ArgumentNullException(nameof(typeEmitter));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if ((attributes & TypeAttributes.Public) == TypeAttributes.Public)
            {
                attributes ^= TypeAttributes.Public;
                attributes |= TypeAttributes.NestedPublic;
            }

            if ((attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic)
            {
                attributes ^= TypeAttributes.NotPublic;
                attributes |= TypeAttributes.NestedPrivate;
            }

            if (baseType is null && interfaces is null)
            {
                return typeEmitter.builder.DefineNestedType(name, attributes);
            }
            else if (interfaces is null || interfaces.Length == 0)
            {
                if (!baseType.IsGenericType)
                {
                    return typeEmitter.builder.DefineNestedType(name, attributes, baseType);
                }

                var genericArguments = baseType.GetGenericArguments();

                if (!Array.Exists(genericArguments, x => x.IsGenericParameter))
                {
                    return typeEmitter.builder.DefineNestedType(name, attributes, baseType);
                }

                var builder = typeEmitter.builder.DefineNestedType(name, attributes);

                var names = new List<string>(genericArguments.Length);

                Array.ForEach(genericArguments, x =>
                {
                    if (x.IsGenericParameter)
                    {
                        names.Add(x.Name);
                    }
                });

                var typeParameterBuilders = builder.DefineGenericParameters(names.ToArray());

                foreach (var item in genericArguments.Zip(typeParameterBuilders, (g, t) =>
                {
                    t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                    t.SetInterfaceConstraints(AdjustGenericConstraints(typeParameterBuilders, genericArguments, g.GetGenericParameterConstraints()));

                    t.SetBaseTypeConstraint(g.BaseType);

                    return true;
                })) { }

                int offset = 0;

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (genericArguments[i].IsGenericParameter)
                    {
                        genericArguments[i] = typeParameterBuilders[i - offset];
                    }
                    else
                    {
                        offset--;
                    }
                }

                builder.SetParent(baseType.GetGenericTypeDefinition().MakeGenericType(genericArguments));

                return builder;
            }
            else
            {
                //? 父类型是否有泛型参数。
                bool flag = false;

                var names = new Dictionary<Type, string>();

                if (baseType?.IsGenericType ?? false)
                {
                    Array.ForEach(baseType.GetGenericArguments(), x =>
                    {
                        if (x.IsGenericParameter)
                        {
                            flag = true;

                            names.Add(x, x.Name);
                        }
                    });
                }

                Array.ForEach(interfaces, x =>
                {
                    if (x.IsGenericType)
                    {
                        Array.ForEach(x.GetGenericArguments(), y =>
                        {
                            if (y.IsGenericParameter && !names.ContainsKey(y))
                            {
                                names.Add(y, y.Name);
                            }
                        });
                    }
                });

                if (names.Count == 0)
                {
                    return typeEmitter.builder.DefineNestedType(name, attributes, baseType, interfaces);
                }

                var builder = flag
                    ? typeEmitter.builder.DefineNestedType(name, attributes)
                    : typeEmitter.builder.DefineNestedType(name, attributes, baseType);

                var typeParameterBuilders = builder.DefineGenericParameters(names.Values.ToArray());

                var genericTypes = names.Keys.ToArray();

                foreach (var item in genericTypes.Zip(typeParameterBuilders, (g, t) =>
                {
                    t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                    t.SetInterfaceConstraints(AdjustGenericConstraints(typeParameterBuilders, genericTypes, g.GetGenericParameterConstraints()));

                    t.SetBaseTypeConstraint(g.BaseType);

                    return true;
                })) { }

                if (flag)
                {
                    var genericArguments = baseType.GetGenericArguments();

                    for (int i = 0; i < genericArguments.Length; i++)
                    {
                        int index = Array.IndexOf(genericTypes, genericArguments[i]);

                        if (index > -1)
                        {
                            genericArguments[i] = typeParameterBuilders[index];
                        }
                    }

                    builder.SetParent(baseType.GetGenericTypeDefinition().MakeGenericType(genericArguments));
                }

                Array.ForEach(interfaces, x =>
                {
                    if (x.IsGenericType)
                    {
                        var genericArguments = x.GetGenericArguments();

                        for (int i = 0; i < genericArguments.Length; i++)
                        {
                            int index = Array.IndexOf(genericTypes, genericArguments[i]);

                            if (index > -1)
                            {
                                genericArguments[i] = typeParameterBuilders[index];
                            }
                        }

                        builder.AddInterfaceImplementation(x.GetGenericTypeDefinition().MakeGenericType(genericArguments));
                    }
                    else
                    {
                        builder.AddInterfaceImplementation(x);
                    }
                });

                return builder;
            }
        }


        /// <summary>
        /// 静态构造函数。
        /// </summary>
        public TypeInitializerEmitter TypeInitializer = new TypeInitializerEmitter();

        /// <summary>
        /// 是否为泛型类。
        /// </summary>
        public bool IsGenericType => builder.IsGenericType;

        /// <summary>
        /// 泛型参数。
        /// </summary>
        /// <returns></returns>
        public Type[] GetGenericArguments() => builder.GetGenericArguments();

        /// <summary>
        /// 父类型。
        /// </summary>
        public Type BaseType
        {
            get
            {
                if (builder.IsInterface)
                {
                    throw new InvalidOperationException("接口不具有“BaseType”属性!");
                }

                return builder.BaseType;
            }
        }

        /// <summary>
        /// 创建字段。
        /// </summary>
        /// <param name="fieldInfo">字段。</param>
        /// <returns></returns>
        public FieldEmitter DefineField(FieldInfo fieldInfo)
        {
            var fieldEmitter = DefineField(fieldInfo.Name, fieldInfo.FieldType, fieldInfo.Attributes);

            if ((fieldInfo.Attributes & FieldAttributes.HasDefault) == FieldAttributes.HasDefault)
            {
                fieldEmitter.SetConstant(fieldInfo.GetRawConstantValue());
            }

            foreach (var attributeData in fieldInfo.GetCustomAttributesData())
            {
                fieldEmitter.SetCustomAttribute(attributeData);
            }

            return fieldEmitter;
        }

        /// <summary>
        /// 创建字段。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="fieldType">类型。</param>
        /// <returns></returns>
        public FieldEmitter DefineField(string name, Type fieldType) => DefineField(name, fieldType, true);

        /// <summary>
        /// 创建字段。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="fieldType">类型。</param>
        /// <param name="serializable">能否序列化。</param>
        /// <returns></returns>
        public FieldEmitter DefineField(string name, Type fieldType, bool serializable)
        {
            var atts = FieldAttributes.Private;

            if (!serializable)
            {
                atts |= FieldAttributes.NotSerialized;
            }

            return DefineField(name, fieldType, atts);
        }

        /// <summary>
        /// 创建字段。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="fieldType">类型。</param>
        /// <param name="atts">属性。</param>
        /// <returns></returns>
        public FieldEmitter DefineField(string name, Type fieldType, FieldAttributes atts)
        {
            name = namingScope.GetUniqueName(name);

            var fieldEmitter = new FieldEmitter(name, fieldType, atts);

            fields.Add(name, fieldEmitter);

            return fieldEmitter;
        }

        /// <summary>
        /// 创建属性。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="attributes">属性。</param>
        /// <param name="propertyType">类型。</param>
        /// <returns></returns>
        public PropertyEmitter DefineProperty(string name, PropertyAttributes attributes, Type propertyType) => DefineProperty(name, attributes, propertyType, null);

        /// <summary>
        /// 创建属性。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="attributes">属性。</param>
        /// <param name="propertyType">类型。</param>
        /// <param name="arguments">参数。</param>
        /// <returns></returns>
        public PropertyEmitter DefineProperty(string name, PropertyAttributes attributes, Type propertyType, Type[] arguments)
        {
            var propEmitter = new PropertyEmitter(name, attributes, propertyType, arguments);
            properties.Add(name, propEmitter);
            return propEmitter;
        }

        /// <summary>
        /// 创建方法。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="attrs">属性。</param>
        /// <param name="returnType">类型。</param>
        /// <returns></returns>
        public MethodEmitter DefineMethod(string name, MethodAttributes attrs, Type returnType)
        {
            var member = new MethodEmitter(name, attrs, returnType);

            methods.Add(member);

            return member;
        }

        /// <summary>
        /// 定义重写方法。
        /// </summary>
        /// <param name="methodInfoDeclaration">被重写的方法。</param>
        /// <param name="attrs">重写方法的方法属性。</param>
        /// <returns></returns>
        public MethodEmitter DefineMethodOverride(ref MethodInfo methodInfoDeclaration, MethodAttributes attrs)
        {
            var parameterInfos = methodInfoDeclaration.GetParameters();

            var parameterTypes = new Type[parameterInfos.Length];

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                parameterTypes[i] = parameterInfos[i].ParameterType;
            }

            MethodInfo methodInfoOriginal = methodInfoDeclaration;

            var methodBuilder = builder.DefineMethod(methodInfoDeclaration.Name, attrs | MethodAttributes.HideBySig, CallingConventions.Standard);

            var overrideEmitter = new MethodOverrideEmitter(methodBuilder, methodInfoOriginal);

            foreach (var parameterInfo in parameterInfos)
            {
                overrideEmitter.DefineParameter(parameterInfo, true);
            }

            bool flag = false;
            var genericArguments = Type.EmptyTypes;
            var newGenericParameters = new GenericTypeParameterBuilder[0];

            if (methodInfoDeclaration.IsGenericMethod)
            {
                genericArguments = methodInfoDeclaration.GetGenericArguments();

                newGenericParameters = methodBuilder.DefineGenericParameters(genericArguments.Select(x => x.Name).ToArray());

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    var g = genericArguments[i];
                    var t = newGenericParameters[i];

                    t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                    t.SetInterfaceConstraints(AdjustGenericConstraints(newGenericParameters, methodInfoDeclaration, genericArguments, g.GetGenericParameterConstraints()));

                    t.SetBaseTypeConstraint(g.BaseType);
                }
            }



            if (methodInfoDeclaration.DeclaringType.IsGenericType && Array.Exists(methodInfoDeclaration.DeclaringType.GetGenericArguments(), x => x.IsGenericParameter))
            {
                Type declaringType = methodBuilder.DeclaringType;
                Type typeDefinition = methodInfoDeclaration.DeclaringType.GetGenericTypeDefinition();

                if (methodInfoDeclaration.DeclaringType.IsClass)
                {
                    do
                    {
                        if (!declaringType.IsGenericType)
                        {
                            continue;
                        }

                        if (declaringType.GetGenericTypeDefinition() == typeDefinition)
                        {
                            break;
                        }

                    } while ((declaringType = declaringType.BaseType) != null);
                }
                else
                {
                    foreach (var interfaceType in methodBuilder.DeclaringType.GetInterfaces())
                    {
                        if (!interfaceType.IsGenericType)
                        {
                            continue;
                        }

                        if (interfaceType.GetGenericTypeDefinition() == typeDefinition)
                        {
                            declaringType = interfaceType;

                            break;
                        }
                    }
                }

                flag = true;

                methodInfoDeclaration = declaringType
                    .GetMethod(methodInfoDeclaration.Name, methodInfoDeclaration
                    .GetParameters()
                    .Select(x => x.ParameterType)
                    .ToArray());
            }

            if (flag)
            {
                methodInfoDeclaration = methodInfoOriginal;
            }

            SetSignature(methodBuilder, methodInfoDeclaration.ReturnType, methodInfoDeclaration.ReturnParameter, parameterTypes, parameterInfos);

            methods.Add(overrideEmitter);

            return overrideEmitter;
        }

        /// <summary>
        /// 声明构造函数。
        /// </summary>
        /// <param name="attributes">属性。</param>
        /// <returns></returns>
        public ConstructorEmitter DefineConstructor(MethodAttributes attributes) => DefineConstructor(attributes, CallingConventions.Standard);

        /// <summary>
        /// 声明构造函数。
        /// </summary>
        /// <param name="attributes">属性。</param>
        /// <param name="conventions">调用约定。</param>
        /// <returns></returns>
        public ConstructorEmitter DefineConstructor(MethodAttributes attributes, CallingConventions conventions)
        {
            var member = new ConstructorEmitter(this, attributes, conventions);
            constructors.Add(member);
            return member;
        }

        /// <summary>
        /// 创建默认构造函数。
        /// </summary>
        /// <returns></returns>
        public void DefineDefaultConstructor()
        {
            constructors.Add(new ConstructorEmitter(this, MethodAttributes.Public));
        }

        /// <summary>
        /// 设置属性标记。
        /// </summary>
        /// <param name="attributeData">属性。</param>
        public void SetCustomAttribute(CustomAttributeData attributeData)
        {
            if (attributeData is null)
            {
                throw new ArgumentNullException(nameof(attributeData));
            }

            builder.SetCustomAttribute(EmitUtils.CreateCustomAttribute(attributeData));
        }

        /// <summary>
        /// 自定义标记。
        /// </summary>
        /// <param name="attribute">标记。</param>
        public void DefineCustomAttribute(CustomAttributeBuilder attribute)
        {
            builder.SetCustomAttribute(attribute);
        }

        /// <summary>
        /// 自定义标记。
        /// </summary>
        /// <typeparam name="TAttribute">标记类型。</typeparam>
        public void DefineCustomAttribute<TAttribute>() where TAttribute : Attribute, new() => DefineCustomAttribute(EmitUtils.CreateCustomAttribute<TAttribute>());

        /// <summary>
        /// 自定义标记。
        /// </summary>
        /// <param name="attributeData">标记信息参数。</param>
        public void DefineCustomAttribute(CustomAttributeData attributeData) => DefineCustomAttribute(EmitUtils.CreateCustomAttribute(attributeData));

        /// <summary>
        /// 在此模块中用指定的名称为私有类型构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径，其中包括命名空间。 name 不能包含嵌入的 null。</param>
        /// <returns>具有指定名称的私有类型。</returns>
        [SecuritySafeCritical]
        public NestedClassEmitter DefineNestedType(string name) => new NestedClassEmitter(this, name);

        /// <summary>
        /// 在给定类型名称和类型特性的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attr">已定义类型的属性。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [SecuritySafeCritical]
        public NestedClassEmitter DefineNestedType(string name, TypeAttributes attr) => new NestedClassEmitter(this, name, attr);

        /// <summary>
        /// 在给定类型名称、类型特性和已定义类型扩展的类型的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attr">与类型关联的属性。</param>
        /// <param name="parent">已定义类型扩展的类型。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [SecuritySafeCritical]
        public NestedClassEmitter DefineNestedType(string name, TypeAttributes attr, Type parent) => new NestedClassEmitter(this, name, attr, parent);

        /// <summary>
        /// 在给定类型名称、特性、已定义类型扩展的类型和已定义类型实现的接口的情况下，构造 TypeBuilder。
        /// </summary>
        /// <param name="name">类型的完整路径。 name 不能包含嵌入的 null。</param>
        /// <param name="attr">与类型关联的特性。</param>
        /// <param name="parent">已定义类型扩展的类型。</param>
        /// <param name="interfaces">类型实现的接口列表。</param>
        /// <returns>用所有请求的特性创建的 TypeBuilder。</returns>
        [ComVisible(true)]
        [SecuritySafeCritical]
        public NestedClassEmitter DefineNestedType(string name, TypeAttributes attr, Type parent, Type[] interfaces) => new NestedClassEmitter(this, name, attr, parent, interfaces);

        /// <summary>
        /// 发行。
        /// </summary>
        protected virtual Type Emit()
        {
            foreach (FieldEmitter emitter in fields.Values)
            {
                emitter.Emit(builder.DefineField(emitter.Name, emitter.RuntimeType, emitter.Attributes));
            }

            TypeInitializer.Emit(builder.DefineTypeInitializer());

            if (!builder.IsInterface && constructors.Count == 0)
            {
                DefineDefaultConstructor();
            }

            foreach (var emitter in abstracts)
            {
                emitter.Emit();
            }

            foreach (ConstructorEmitter emitter in constructors)
            {
                emitter.Emit(builder.DefineConstructor(emitter.Attributes, emitter.Conventions, emitter.Parameters.Select(x => x.RuntimeType).ToArray()));
            }

            foreach (MethodEmitter emitter in methods)
            {
                emitter.Emit(builder);
            }

            foreach (PropertyEmitter emitter in properties.Values)
            {
                emitter.Emit(builder.DefineProperty(emitter.Name, emitter.Attributes, emitter.RuntimeType, emitter.ParameterTypes));
            }

#if NETSTANDARD2_0_OR_GREATER
            return builder.CreateTypeInfo().AsType();
#else
            return builder.CreateType();
#endif
        }

        private static Type AdjustConstraintToNewGenericParameters(Type constraint, Type[] originalGenericParameters, GenericTypeParameterBuilder[] newGenericParameters)
        {
            if (constraint.IsGenericType)
            {
                var genericArgumentsOfConstraint = constraint.GetGenericArguments();

                for (var i = 0; i < genericArgumentsOfConstraint.Length; ++i)
                {
                    genericArgumentsOfConstraint[i] =
                        AdjustConstraintToNewGenericParameters(genericArgumentsOfConstraint[i], originalGenericParameters, newGenericParameters);
                }

                return constraint.GetGenericTypeDefinition().MakeGenericType(genericArgumentsOfConstraint);
            }
            else
            {
                return constraint;
            }
        }

        private static Type[] AdjustGenericConstraints(GenericTypeParameterBuilder[] newGenericParameters, Type[] originalGenericArguments, Type[] constraints)
        {
            Type[] adjustedConstraints = new Type[constraints.Length];
            for (var i = 0; i < constraints.Length; i++)
            {
                adjustedConstraints[i] = AdjustConstraintToNewGenericParameters(constraints[i], originalGenericArguments, newGenericParameters);
            }
            return adjustedConstraints;
        }

        private static Type AdjustConstraintToNewGenericParameters(
            Type constraint, MethodInfo methodToCopyGenericsFrom, Type[] originalGenericParameters,
            GenericTypeParameterBuilder[] newGenericParameters)
        {
            if (constraint.IsGenericType)
            {
                var genericArgumentsOfConstraint = constraint.GetGenericArguments();

                for (var i = 0; i < genericArgumentsOfConstraint.Length; ++i)
                {
                    genericArgumentsOfConstraint[i] =
                        AdjustConstraintToNewGenericParameters(genericArgumentsOfConstraint[i], methodToCopyGenericsFrom,
                                                               originalGenericParameters, newGenericParameters);
                }
                return constraint.GetGenericTypeDefinition().MakeGenericType(genericArgumentsOfConstraint);
            }
            else if (constraint.IsGenericParameter)
            {
                if (constraint.DeclaringMethod is null)
                {
                    Trace.Assert(constraint.DeclaringType.IsGenericTypeDefinition);
                    Trace.Assert(methodToCopyGenericsFrom.DeclaringType.IsGenericType
                                 && constraint.DeclaringType == methodToCopyGenericsFrom.DeclaringType.GetGenericTypeDefinition(),
                                 "When a generic method parameter has a constraint on a generic type parameter, the generic type must be the declaring typer of the method.");

                    var index = Array.IndexOf(constraint.DeclaringType.GetGenericArguments(), constraint);
                    Trace.Assert(index != -1, "The generic parameter comes from the given type.");

                    var genericArguments = methodToCopyGenericsFrom.DeclaringType.GetGenericArguments();

                    return genericArguments[index]; // these are the actual, concrete types
                }
                else
                {
                    var index = Array.IndexOf(originalGenericParameters, constraint);
                    Trace.Assert(index != -1,
                                 "When a generic method parameter has a constraint on another method parameter, both parameters must be declared on the same method.");
                    return newGenericParameters[index];
                }
            }
            else
            {
                return constraint;
            }
        }

        private static Type[] AdjustGenericConstraints(GenericTypeParameterBuilder[] newGenericParameters, MethodInfo methodInfo, Type[] originalGenericArguments, Type[] constraints)
        {
            Type[] adjustedConstraints = new Type[constraints.Length];
            for (var i = 0; i < constraints.Length; i++)
            {
                adjustedConstraints[i] = AdjustConstraintToNewGenericParameters(constraints[i], methodInfo, originalGenericArguments, newGenericParameters);
            }
            return adjustedConstraints;
        }

        private static void SetSignature(MethodBuilder builder, Type returnType, ParameterInfo returnParameter, Type[] parameters,
                                  ParameterInfo[] baseMethodParameters)
        {
            Type[] returnRequiredCustomModifiers;
            Type[] returnOptionalCustomModifiers;
            Type[][] parametersRequiredCustomModifiers;
            Type[][] parametersOptionalCustomModifiers;

            returnRequiredCustomModifiers = returnParameter.GetRequiredCustomModifiers();
            Array.Reverse(returnRequiredCustomModifiers);

            returnOptionalCustomModifiers = returnParameter.GetOptionalCustomModifiers();
            Array.Reverse(returnOptionalCustomModifiers);

            int parameterCount = baseMethodParameters.Length;
            parametersRequiredCustomModifiers = new Type[parameterCount][];
            parametersOptionalCustomModifiers = new Type[parameterCount][];
            for (int i = 0; i < parameterCount; ++i)
            {
                parametersRequiredCustomModifiers[i] = baseMethodParameters[i].GetRequiredCustomModifiers();
                Array.Reverse(parametersRequiredCustomModifiers[i]);

                parametersOptionalCustomModifiers[i] = baseMethodParameters[i].GetOptionalCustomModifiers();
                Array.Reverse(parametersOptionalCustomModifiers[i]);
            }

            builder.SetSignature(
                returnType,
                returnRequiredCustomModifiers,
                returnOptionalCustomModifiers,
                parameters,
                parametersRequiredCustomModifiers,
                parametersOptionalCustomModifiers);
        }

        private static bool HasGenericParameter(Type type, Type[] genericArguments)
        {
            if (type.IsGenericParameter)
            {
                return genericArguments is null || Array.IndexOf(genericArguments, type) > -1;
            }

            if (type.IsGenericType)
            {
                Debug.Assert(type.IsGenericTypeDefinition == false);

                return Array.Exists(type.GetGenericArguments(), x => HasGenericParameter(x, genericArguments));
            }

            if (type.IsArray || type.IsByRef)
            {
                return HasGenericParameter(type.GetElementType(), genericArguments);
            }

            return false;
        }

        private static Type MakeGenericParameter(Type type, Type[] genericArguments)
        {
            if (type.IsGenericParameter)
            {
                return null;
            }

            if (type.IsGenericType)
            {
                Debug.Assert(type.IsGenericTypeDefinition == false);

                //return Array.Exists(type.GetGenericArguments(), x => HasGenericParameter(x, genericArguments));
            }

            if (type.IsArray || type.IsByRef)
            {
                //return HasGenericParameter(type.GetElementType(), genericArguments);
            }

            return type;
        }

        /// <summary>
        /// 是否已创建。
        /// </summary>
        /// <returns></returns>
        public bool IsCreated() => builder.IsCreated();
    }
}

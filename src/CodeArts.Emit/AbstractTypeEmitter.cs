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
using System.Text.RegularExpressions;

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

                ilg.Emit(OpCodes.Ret);
            }
        }

        /// <summary>
        /// 方法。
        /// </summary>
        private sealed class MethodOverrideEmitter : MethodEmitter
        {
            private readonly MethodBuilder methodBuilder;
            private readonly MethodInfo methodInfoDeclaration;

            public MethodOverrideEmitter(MethodBuilder methodBuilder, MethodInfo methodInfoDeclaration, Type returnType) : base(methodBuilder.Name, methodBuilder.Attributes, returnType)
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

            public override bool IsGenericMethod => methodInfoDeclaration.IsGenericMethod;

            public override Type[] GetGenericArguments() => methodBuilder.GetGenericArguments();

            public ParameterEmitter DefineParameter(ParameterInfo parameterInfo, Type parameterType)
            {
                var parameter = base.DefineParameter(parameterType, parameterInfo.Attributes, parameterInfo.Name);

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

            public override ParameterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string name)
            {
                throw new AstException("重写方法不支持自定义参数!");
            }

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
            this.namingScope = namingScope?.BeginScope() ?? throw new ArgumentNullException(nameof(namingScope));
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
        protected AbstractTypeEmitter(AbstractTypeEmitter typeEmitter, string name, TypeAttributes attributes, Type baseType, Type[] interfaces) : this(DefineNestedTypeBuilder(typeEmitter.builder, name, attributes, baseType, interfaces), typeEmitter.namingScope)
        {
            typeEmitter.abstracts.Add(this);
        }

        private static readonly Regex NamingPattern = new Regex("[^0-9a-zA-Z]+", RegexOptions.Singleline | RegexOptions.Compiled);
        private static TypeBuilder DefineNestedTypeBuilder(TypeBuilder typeBuilder, string name, TypeAttributes attributes, Type baseType, Type[] interfaces)
        {
            if (typeBuilder is null)
            {
                throw new ArgumentNullException(nameof(typeBuilder));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            name = NamingPattern.Replace(name, string.Empty);

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
                return typeBuilder.DefineNestedType(name, attributes);
            }
            else if (interfaces is null || interfaces.Length == 0)
            {
                if (!baseType.IsGenericType)
                {
                    return typeBuilder.DefineNestedType(name, attributes, baseType);
                }

                var genericArguments = baseType.GetGenericArguments();

                if (!Array.Exists(genericArguments, x => x.IsGenericParameter))
                {
                    return typeBuilder.DefineNestedType(name, attributes, baseType);
                }

                var builder = typeBuilder.DefineNestedType(name, attributes);

                var names = new List<string>(genericArguments.Length);

                Array.ForEach(genericArguments, x =>
                {
                    if (x.IsGenericParameter)
                    {
                        names.Add(x.Name);
                    }
                });

                var typeParameterBuilders = builder.DefineGenericParameters(names.ToArray());

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    var g = genericArguments[i];
                    var t = typeParameterBuilders[i];

                    t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                    t.SetInterfaceConstraints(AdjustGenericConstraints(typeParameterBuilders, genericArguments, g.GetGenericParameterConstraints()));

                    //? 避免重复约束。 T2 where T, T, new()
                    if (g.BaseType.IsGenericParameter)
                    {
                        continue;
                    }

                    if (HasGenericParameter(g.BaseType))
                    {
                        t.SetBaseTypeConstraint(MakeGenericParameter(g.BaseType, typeParameterBuilders));
                    }
                    else
                    {
                        t.SetBaseTypeConstraint(g.BaseType);
                    }
                }

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
                    return typeBuilder.DefineNestedType(name, attributes, baseType, interfaces);
                }

                var builder = flag
                    ? typeBuilder.DefineNestedType(name, attributes)
                    : typeBuilder.DefineNestedType(name, attributes, baseType);

                var typeParameterBuilders = builder.DefineGenericParameters(names.Values.ToArray());

                var genericTypes = names.Keys.ToArray();

                for (int i = 0; i < genericTypes.Length; i++)
                {
                    var g = genericTypes[i];
                    var t = typeParameterBuilders[i];

                    t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                    t.SetInterfaceConstraints(AdjustGenericConstraints(typeParameterBuilders, genericTypes, g.GetGenericParameterConstraints()));

                    //? 避免重复约束。 T2 where T, T, new()
                    if (g.BaseType.IsGenericParameter)
                    {
                        continue;
                    }

                    if (HasGenericParameter(g.BaseType))
                    {
                        t.SetBaseTypeConstraint(MakeGenericParameter(g.BaseType, typeParameterBuilders));
                    }
                    else
                    {
                        t.SetBaseTypeConstraint(g.BaseType);
                    }
                }

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

            var methodBuilder = builder.DefineMethod(methodInfoDeclaration.Name, attrs | MethodAttributes.HideBySig, CallingConventions.Standard);

            var genericArguments = Type.EmptyTypes;
            Type returnType = methodInfoDeclaration.ReturnType;
            Type runtimeType = methodInfoDeclaration.ReturnType;
            ParameterInfo returnParameter = methodInfoDeclaration.ReturnParameter;
            GenericTypeParameterBuilder[] newGenericParameters = new GenericTypeParameterBuilder[0];

            MethodInfo methodInfoOriginal = methodInfoDeclaration;

            if (HasGenericParameter(methodInfoOriginal.DeclaringType))
            {
                Type declaringType = builder;

                var typeDefinition = methodInfoOriginal
                        .DeclaringType
                        .GetGenericTypeDefinition();

                if (methodInfoOriginal.DeclaringType.IsClass)
                {
                    while ((declaringType = declaringType.BaseType) != null)
                    {
                        if (declaringType.IsGenericType && declaringType.GetGenericTypeDefinition() == typeDefinition)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var interfaceType in builder.GetInterfaces())
                    {
                        if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeDefinition)
                        {
                            declaringType = interfaceType;

                            break;
                        }
                    }
                }

                bool hasDeclaringTypes = false;

                Type[] declaringTypes = methodInfoOriginal
                    .DeclaringType
                    .GetGenericArguments();

                Type[] declaringTypeParameters = declaringType.GetGenericArguments();

                if (methodInfoOriginal.IsGenericMethod)
                {
                    genericArguments = methodInfoOriginal.GetGenericArguments();

                    newGenericParameters = methodBuilder.DefineGenericParameters(genericArguments.Select(x => x.Name).ToArray());

                    for (int i = 0; i < genericArguments.Length; i++)
                    {
                        var g = genericArguments[i];
                        var t = newGenericParameters[i];

                        t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                        t.SetInterfaceConstraints(AdjustGenericConstraints(newGenericParameters, methodInfoOriginal, genericArguments, g.GetGenericParameterConstraints()));

                        //? 避免重复约束。 T2 where T, T, new()
                        if (g.BaseType.IsGenericParameter)
                        {
                            continue;
                        }

                        if (HasGenericParameter(g.BaseType))
                        {
                            t.SetBaseTypeConstraint(MakeGenericParameter(g.BaseType, genericArguments, declaringTypeParameters, newGenericParameters));
                        }
                        else
                        {
                            t.SetBaseTypeConstraint(g.BaseType);
                        }
                    }

                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        if (HasGenericParameter(parameterTypes[i]))
                        {
                            parameterTypes[i] = MakeGenericParameter(parameterTypes[i], genericArguments, declaringTypeParameters, newGenericParameters);
                        }
                    }

                    var genericMethod = methodInfoDeclaration.MakeGenericMethod(newGenericParameters);

                    if (hasDeclaringTypes = HasGenericParameter(returnType, declaringTypes))
                    {
                        runtimeType = MakeGenericParameter(returnType, newGenericParameters, declaringTypeParameters);

                        returnType = MakeGenericParameter(returnType, genericArguments, declaringTypeParameters, newGenericParameters);
                    }
                    else if (HasGenericParameter(returnType))
                    {
                        runtimeType = genericMethod.ReturnType;

                        returnType = MakeGenericParameter(returnType, genericArguments, declaringTypeParameters, newGenericParameters);
                    }

                    methodInfoDeclaration = new DynamicMethod(methodInfoOriginal, genericMethod, declaringType, runtimeType, declaringTypeParameters, hasDeclaringTypes);
                }
                else
                {
                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        if (HasGenericParameter(parameterTypes[i]))
                        {
                            parameterTypes[i] = MakeGenericParameter(parameterTypes[i], declaringTypeParameters);
                        }
                    }

                    if (hasDeclaringTypes = HasGenericParameter(returnType, declaringTypes))
                    {
                        runtimeType = returnType = MakeGenericParameter(returnType, declaringTypeParameters);
                    }
                    else if (HasGenericParameter(returnType))
                    {
                        returnType = MakeGenericParameter(returnType, declaringTypeParameters);
                    }

                    methodInfoDeclaration = new DynamicMethod(methodInfoOriginal, declaringType, runtimeType, declaringTypeParameters, hasDeclaringTypes);
                }
            }
            else if (methodInfoOriginal.IsGenericMethod)
            {
                genericArguments = methodInfoOriginal.GetGenericArguments();

                newGenericParameters = methodBuilder.DefineGenericParameters(genericArguments.Select(x => x.Name).ToArray());

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    var g = genericArguments[i];

                    var t = newGenericParameters[i];

                    t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                    t.SetInterfaceConstraints(AdjustGenericConstraints(newGenericParameters, methodInfoOriginal, genericArguments, g.GetGenericParameterConstraints()));

                    //? 避免重复约束。 T2 where T, T, new()
                    if (g.BaseType.IsGenericParameter)
                    {
                        continue;
                    }

                    if (HasGenericParameter(g.BaseType))
                    {
                        t.SetBaseTypeConstraint(MakeGenericParameter(g.BaseType, genericArguments));
                    }
                    else
                    {
                        t.SetBaseTypeConstraint(g.BaseType);
                    }
                }

                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (HasGenericParameter(parameterTypes[i]))
                    {
                        parameterTypes[i] = MakeGenericParameter(parameterTypes[i], newGenericParameters);
                    }
                }

                var genericMethod = methodInfoOriginal.MakeGenericMethod(newGenericParameters);

                if (HasGenericParameter(returnType))
                {
                    runtimeType = methodInfoOriginal.ReturnType;

                    returnType = MakeGenericParameter(returnType, newGenericParameters);
                }

                methodInfoDeclaration = new DynamicMethod(methodInfoOriginal, genericMethod, methodInfoOriginal.DeclaringType, runtimeType, Type.EmptyTypes, false);
            }

            var overrideEmitter = new MethodOverrideEmitter(methodBuilder, methodInfoOriginal, runtimeType);

            for (int i = 0; i < parameterInfos.Length; i++)
            {
                overrideEmitter.DefineParameter(parameterInfos[i], parameterTypes[i]);
            }

            Type[] returnRequiredCustomModifiers;
            Type[] returnOptionalCustomModifiers;
            Type[][] parametersRequiredCustomModifiers;
            Type[][] parametersOptionalCustomModifiers;

            returnRequiredCustomModifiers = returnParameter.GetRequiredCustomModifiers();
            Array.Reverse(returnRequiredCustomModifiers);

            returnOptionalCustomModifiers = returnParameter.GetOptionalCustomModifiers();
            Array.Reverse(returnOptionalCustomModifiers);

            int parameterCount = parameterInfos.Length;
            parametersRequiredCustomModifiers = new Type[parameterCount][];
            parametersOptionalCustomModifiers = new Type[parameterCount][];
            for (int i = 0; i < parameterCount; ++i)
            {
                parametersRequiredCustomModifiers[i] = parameterInfos[i].GetRequiredCustomModifiers();
                Array.Reverse(parametersRequiredCustomModifiers[i]);

                parametersOptionalCustomModifiers[i] = parameterInfos[i].GetOptionalCustomModifiers();
                Array.Reverse(parametersOptionalCustomModifiers[i]);
            }

            methodBuilder.SetSignature(
                returnType,
                returnRequiredCustomModifiers,
                returnOptionalCustomModifiers,
                parameterTypes,
                parametersRequiredCustomModifiers,
                parametersOptionalCustomModifiers);

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

        private static bool HasGenericParameter(Type type)
        {
            if (type.IsGenericParameter)
            {
                return true;
            }

            if (type.IsGenericType)
            {
                return Array.Exists(type.GetGenericArguments(), HasGenericParameter);
            }

            if (type.IsArray || type.IsByRef)
            {
                return HasGenericParameter(type.GetElementType());
            }

            return false;
        }

        private static bool HasGenericParameter(Type type, Type[] declaringTypes)
        {
            if (type.IsGenericParameter)
            {
                return Array.IndexOf(declaringTypes, type) > -1;
            }

            if (type.IsGenericType)
            {
                return Array.Exists(type.GetGenericArguments(), x => HasGenericParameter(x, declaringTypes));
            }

            if (type.IsArray || type.IsByRef)
            {
                return HasGenericParameter(type.GetElementType(), declaringTypes);
            }

            return false;
        }

        private static Type MakeGenericParameter(Type type, Type[] typeParameterBuilders)
        {
            if (type.IsGenericParameter)
            {
                return typeParameterBuilders[type.GenericParameterPosition];
            }

            if (type.IsGenericType)
            {
                bool flag = false;

                var genericArguments = type.GetGenericArguments();

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (HasGenericParameter(genericArguments[i]))
                    {
                        flag = true;
                        genericArguments[i] = MakeGenericParameter(genericArguments[i], typeParameterBuilders);
                    }

                }

                return flag
                    ? type.GetGenericTypeDefinition().MakeGenericType(genericArguments)
                    : type;
            }

            if (type.IsArray)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), typeParameterBuilders);
                int rank = type.GetArrayRank();

                return rank == 1
                    ? elementType.MakeArrayType()
                    : elementType.MakeArrayType(rank);
            }

            if (type.IsByRef)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), typeParameterBuilders);

                return elementType.MakeByRefType();
            }

            return type;
        }

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

        private static Type MakeGenericParameter(Type type, Type[] genericArguments, Type[] declaringTypeParameters, GenericTypeParameterBuilder[] newGenericParameters)
        {
            if (type.IsGenericParameter)
            {
                if (Array.IndexOf(genericArguments, type) > -1)
                {
                    return newGenericParameters[type.GenericParameterPosition];
                }

                return declaringTypeParameters[type.GenericParameterPosition];
            }

            if (type.IsGenericType)
            {
                Debug.Assert(type.IsGenericTypeDefinition == false);

                bool flag = false;

                var genericArguments2 = type.GetGenericArguments();

                for (int i = 0; i < genericArguments2.Length; i++)
                {
                    if (HasGenericParameter(genericArguments2[i]))
                    {
                        genericArguments2[i] = MakeGenericParameter(genericArguments2[i], genericArguments, declaringTypeParameters, newGenericParameters);
                    }

                }

                return flag
                    ? type.GetGenericTypeDefinition().MakeGenericType(genericArguments2)
                    : type;
            }

            if (type.IsArray)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), genericArguments, declaringTypeParameters, newGenericParameters);
                int rank = type.GetArrayRank();

                return rank == 1
                    ? elementType.MakeArrayType()
                    : elementType.MakeArrayType(rank);
            }

            if (type.IsByRef)
            {
                Type elementType = MakeGenericParameter(type.GetElementType(), genericArguments, declaringTypeParameters, newGenericParameters);

                return elementType.MakeByRefType();
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

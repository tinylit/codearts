using CodeArts.Emit.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Emit
{
    /// <summary>
    /// 抽象类。
    /// </summary>
    public abstract class AbstractTypeEmitter
    {
        private readonly TypeBuilder builder;
        private readonly List<MethodEmitter> methods = new List<MethodEmitter>();
        private readonly List<ConstructorEmitter> constructors = new List<ConstructorEmitter>();
        private readonly Dictionary<MethodEmitter, MethodInfo> overrides = new Dictionary<MethodEmitter, MethodInfo>();
        private readonly Dictionary<string, FieldEmitter> fields = new Dictionary<string, FieldEmitter>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PropertyEmitter> properties = new Dictionary<string, PropertyEmitter>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="builder">类型构造器</param>
        protected AbstractTypeEmitter(TypeBuilder builder)
        {
            this.builder = builder;
        }

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
        /// <param name="name">名称。</param>
        /// <param name="fieldType">类型。</param>
        /// <returns></returns>
        public FieldEmitter DefineField(string name, Type fieldType)
        {
            return DefineField(name, fieldType, true);
        }

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
            var reference = new FieldEmitter(name, fieldType, atts);

            fields.Add(name, reference);

            return reference;
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
        /// 实现接口方法或重写方法。
        /// </summary>
        /// <param name="methodEmitter">方法。</param>
        /// <param name="methodInfoDeclaration">被重写或实现的方法。</param>
        public void DefineMethodOverride(MethodEmitter methodEmitter, MethodInfo methodInfoDeclaration)
        {
            if (methodEmitter is null)
            {
                throw new ArgumentNullException(nameof(methodEmitter));
            }

            if (methodInfoDeclaration is null)
            {
                throw new ArgumentNullException(nameof(methodInfoDeclaration));
            }

            overrides.Add(methodEmitter, methodInfoDeclaration);
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
            var member = new ConstructorEmitter(BaseType, attributes, conventions);
            constructors.Add(member);
            return member;
        }

        /// <summary>
        /// 创建默认构造函数。
        /// </summary>
        /// <returns></returns>
        public void DefineDefaultConstructor()
        {
            constructors.Add(new ConstructorEmitter(BaseType, MethodAttributes.Public));
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
        /// 发行。
        /// </summary>
        protected virtual Type Emit()
        {
            if (!builder.IsInterface && constructors.Count == 0)
            {
                DefineDefaultConstructor();
            }

            foreach (FieldEmitter emitter in fields.Values)
            {
                emitter.Emit(builder.DefineField(emitter.Name, emitter.ReturnType, emitter.Attributes));
            }

            foreach (ConstructorEmitter emitter in constructors)
            {
                emitter.Emit(builder.DefineConstructor(emitter.Attributes, emitter.Conventions, emitter.Parameters.Select(x => x.ReturnType).ToArray()));
            }

            foreach (MethodEmitter emitter in methods)
            {
                if (overrides.TryGetValue(emitter, out MethodInfo methodInfo))
                {
                    var parameters = methodInfo.GetParameters();
                    var parameterTypes = parameters.Select(x => x.ParameterType).ToArray();

                    var method = builder.DefineMethod(emitter.Name, emitter.Attributes | MethodAttributes.NewSlot | MethodAttributes.HideBySig, CallingConventions.Standard);

                    if (methodInfo.IsGenericMethod)
                    {
                        var genericArguments = methodInfo.GetGenericArguments();

                        var newGenericParameters = method.DefineGenericParameters(genericArguments.Select(x => x.Name).ToArray());

                        foreach (var item in genericArguments.Zip(newGenericParameters, (g, t) =>
                        {
                            t.SetGenericParameterAttributes(g.GenericParameterAttributes);

                            t.SetInterfaceConstraints(g.GetGenericParameterConstraints());

                            t.SetBaseTypeConstraint(g.BaseType);

                            return true;
                        })) { }
                    }

                    method.SetReturnType(methodInfo.ReturnType);

                    method.SetParameters(parameterTypes);

                    emitter.Emit(method);

                    builder.DefineMethodOverride(method, methodInfo);
                }
                else
                {
                    var method = builder.DefineMethod(emitter.Name, emitter.Attributes, CallingConventions.Standard, emitter.ReturnType, emitter.Parameters.Select(x => x.ReturnType).ToArray());

                    emitter.Emit(method);
                }
            }

            foreach (PropertyEmitter emitter in properties.Values)
            {
                emitter.Emit(builder.DefineProperty(emitter.Name, emitter.Attributes, emitter.ReturnType, emitter.ParameterTypes));
            }

#if NETSTANDARD2_0
            return builder.CreateTypeInfo().AsType();
#else
            return builder.CreateType();
#endif
        }

        /// <summary>
        /// 是否已创建。
        /// </summary>
        /// <returns></returns>
        public bool IsCreated() => builder.IsCreated();

        /// <summary>
        /// 创建类型。
        /// </summary>
        /// <returns></returns>
        public virtual Type CreateType() => Emit();
    }
}

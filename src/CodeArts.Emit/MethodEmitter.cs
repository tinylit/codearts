using CodeArts.Emit.Expressions;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 方法。
    /// </summary>
    [DebuggerDisplay("{ReturnType.Name} {Name}({ParemetersNames})")]
    public class MethodEmitter : BlockAst
    {
        private MethodBuilder methodBuilder;
        private int parameterIndex = 0;
        private readonly List<ParameterEmitter> parameters = new List<ParameterEmitter>();
        private readonly List<CustomAttributeBuilder> customAttributes = new List<CustomAttributeBuilder>();

        private string ParemetersNames => string.Join(",", parameters.Select(x => string.Concat(x.RuntimeType.Name, " ", x.ParameterName)));

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">方法的名称。</param>
        /// <param name="attributes">方法的属性。</param>
        /// <param name="returnType">方法的返回类型。</param>
        public MethodEmitter(string name, MethodAttributes attributes, Type returnType) : base(returnType)
        {
            Name = name;
            Attributes = attributes;
        }

        /// <summary>
        /// 是否为泛型方法。
        /// </summary>
        public virtual bool IsGenericMethod => false;

        /// <summary>
        /// 泛型参数。
        /// </summary>
        /// <returns></returns>
        public virtual Type[] GetGenericArguments() => null;

        /// <summary>
        /// 成员。
        /// </summary>
        internal MethodBuilder Value => methodBuilder ?? throw new NotImplementedException();

        /// <summary>
        /// 方法的名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 方法的属性。
        /// </summary>
        public MethodAttributes Attributes { get; }

        /// <summary>
        /// 参数。
        /// </summary>
        public ParameterEmitter[] GetParameters() => parameters.ToArray();

        /// <summary>
        /// 声明参数。
        /// </summary>
        /// <param name="parameterInfo">参数。</param>
        /// <returns></returns>
        public ParameterEmitter DefineParameter(ParameterInfo parameterInfo)
        {
            var parameter = DefineParameter(parameterInfo.ParameterType, parameterInfo.Attributes, parameterInfo.Name);

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

        /// <summary>
        /// 声明参数。
        /// </summary>
        /// <param name="parameterType">参数类型。</param>
        /// <param name="strParamName">名称。</param>
        /// <returns></returns>
        public ParameterEmitter DefineParameter(Type parameterType, string strParamName) => DefineParameter(parameterType, ParameterAttributes.None, strParamName);

        /// <summary>
        /// 声明参数。
        /// </summary>
        /// <param name="parameterType">参数类型。</param>
        /// <param name="attributes">属性。</param>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        public ParameterEmitter DefineParameter(Type parameterType, ParameterAttributes attributes, string name)
        {
            var parameter = new ParameterEmitter(parameterType, ++parameterIndex, attributes, name);
            parameters.Add(parameter);
            return parameter;
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

            customAttributes.Add(EmitUtils.CreateCustomAttribute(attributeData));
        }

        /// <summary>
        /// 设置属性标记。
        /// </summary>
        /// <param name="customBuilder">属性。</param>
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder is null)
            {
                throw new ArgumentNullException(nameof(customBuilder));
            }

            customAttributes.Add(customBuilder);
        }

        /// <summary>
        /// 发行方法。
        /// </summary>
        /// <param name="methodBuilder">方法。</param>
        protected virtual void Emit(MethodBuilder methodBuilder)
        {
            this.methodBuilder = methodBuilder;

            foreach (var item in customAttributes)
            {
                methodBuilder.SetCustomAttribute(item);
            }

            var ilg = methodBuilder.GetILGenerator();

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

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="builder">构造器。</param>
        public virtual void Emit(TypeBuilder builder)
        {
            int index = 0;

            var parameterTypes = new Type[parameters.Count];

            foreach (var parameterEmitter in parameters)
            {
                parameterTypes[index++] = parameterEmitter.RuntimeType;
            }

            var methodBuilder = builder.DefineMethod(Name, Attributes, CallingConventions.Standard, RuntimeType, parameterTypes);

            foreach (var parameter in parameters)
            {
                parameter.Emit(methodBuilder.DefineParameter(parameter.Position, parameter.Attributes, parameter.ParameterName));
            }

            Emit(methodBuilder);
        }
    }
}

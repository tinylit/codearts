using CodeArts.Emit.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 属性。
    /// </summary>
    [DebuggerDisplay("{Name} ({ReturnType})")]
    public class PropertyEmitter : MemberAst
    {
        private MethodEmitter _Getter;
        private MethodEmitter _Setter;
        private object defaultValue;
        private bool hasDefaultValue = false;
        private readonly List<CustomAttributeBuilder> customAttributes = new List<CustomAttributeBuilder>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">属性的名称。</param>
        /// <param name="attributes">属性的特性。</param>
        /// <param name="returnType">属性的返回类型。</param>
        public PropertyEmitter(string name, PropertyAttributes attributes, Type returnType) : this(name, attributes, returnType, null)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="name">属性的名称。</param>
        /// <param name="attributes">属性的特性。</param>
        /// <param name="returnType">属性的返回类型。</param>
        /// <param name="parameterTypes">属性的参数类型。</param>
        public PropertyEmitter(string name, PropertyAttributes attributes, Type returnType, Type[] parameterTypes) : base(returnType)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Attributes = attributes;
            ParameterTypes = parameterTypes;
        }

        /// <summary>
        /// 属性的名称。
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 属性的特性。
        /// </summary>
        public PropertyAttributes Attributes { get; }

        /// <summary>
        /// 属性的参数类型。
        /// </summary>
        public Type[] ParameterTypes { get; }

        /// <summary>
        /// 是否可写。
        /// </summary>
        public override bool CanWrite => !(_Setter is null);

        /// <summary>
        /// 设置Get方法。
        /// </summary>
        /// <param name="getter">数据获取器。</param>
        /// <returns></returns>
        public PropertyEmitter SetGetMethod(MethodEmitter getter)
        {
            if (getter is null)
            {
                throw new ArgumentNullException(nameof(getter));
            }

            _Getter = getter;

            return this;
        }
        /// <summary>
        /// 创建Set方法。
        /// </summary>
        /// <param name="setter">数据设置器。</param>
        /// <returns></returns>
        public PropertyEmitter SetSetMethod(MethodEmitter setter)
        {
            if (setter is null)
            {
                throw new ArgumentNullException(nameof(setter));
            }

            _Setter = setter;

            return this;
        }

        /// <summary>
        /// 设置默认值。
        /// </summary>
        /// <param name="defaultValue">默认值。</param>
        public void SetConstant(object defaultValue)
        {
            hasDefaultValue = true;

            this.defaultValue = EmitUtils.SetConstantOfType(defaultValue, ReturnType);
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
        /// 发行。
        /// </summary>
        /// <param name="builder">类型构造器。</param>
        public void Emit(PropertyBuilder builder)
        {
            if (_Getter is null && _Setter is null)
            {
                throw new InvalidOperationException($"属性不能既不可读，也不可写!");
            }

            if (_Getter != null)
            {
                builder.SetGetMethod(_Getter.Value);
            }

            if (_Setter != null)
            {
                builder.SetSetMethod(_Setter.Value);
            }

            if (hasDefaultValue)
            {
                builder.SetConstant(defaultValue);
            }

            foreach (var item in customAttributes)
            {
                builder.SetCustomAttribute(item);
            }
        }

        /// <summary>
        /// 获取成员数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected override void MemberLoad(ILGenerator ilg)
        {
            if (_Getter is null)
            {
                throw new AstException($"属性“{Name}”不可读!");
            }

            ilg.Emit(OpCodes.Callvirt, _Getter.Value);
        }


        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void AssignCore(ILGenerator ilg, AstExpression value)
        {
            value.Load(ilg);

            ilg.Emit(OpCodes.Call, _Setter.Value);
        }
    }
}

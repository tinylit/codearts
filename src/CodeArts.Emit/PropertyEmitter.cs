using CodeArts.Emit.Expressions;
using System;
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
        /// 设置Get方法。
        /// </summary>
        /// <param name="getter">数据获取器</param>
        /// <returns></returns>
        public PropertyEmitter SetGetMethod(MethodEmitter getter)
        {
            if (getter is null)
            {
                throw new ArgumentNullException(nameof(getter));
            }

            if (_Getter != null)
            {
                throw new InvalidOperationException("已存在一个数据获取器!");
            }

            _Getter = getter;

            return this;
        }
        /// <summary>
        /// 创建Set方法。
        /// </summary>
        /// <param name="setter">数据设置器</param>
        /// <returns></returns>
        public PropertyEmitter SetSetMethod(MethodEmitter setter)
        {
            if (setter is null)
            {
                throw new ArgumentNullException(nameof(setter));
            }

            if (_Setter != null)
            {
                throw new InvalidOperationException("已存在一个数据设置器!");
            }

            _Setter = setter;

            return this;
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
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
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
        public override void Assign(ILGenerator ilg)
        {
            if (_Setter is null)
            {
                throw new AstException($"属性“{Name}”不可写!");
            }

            ilg.Emit(OpCodes.Call, _Setter.Value);
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void AssignCore(ILGenerator ilg, AstExpression value)
        {
            if (_Setter is null)
            {
                throw new AstException($"属性“{Name}”不可写!");
            }

            value.Load(ilg);

            ilg.Emit(OpCodes.Call, _Setter.Value);
        }
    }
}

using CodeArts.Emit.Expressions;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 属性。
    /// </summary>
    public class PropertyEmitter : MemberExpression
    {
        private MethodEmitter GetMethod;
        private MethodEmitter SetMethod;

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
        /// 创建Get方法。
        /// </summary>
        /// <param name="attrs">属性。</param>
        /// <returns></returns>
        public MethodEmitter CreateGetMethod(MethodAttributes attrs)
        {
            if (GetMethod != null)
            {
                throw new InvalidOperationException("A get method exists");
            }

            GetMethod = new MethodEmitter("Get" + Name, attrs, ReturnType);

            if (ParameterTypes != null)
            {
                int i = 0;
                Array.ForEach(ParameterTypes, type =>
                {
                    GetMethod.DefineParameter(type, ParameterAttributes.None, Name + "_" + i++);
                });
            }

            return GetMethod;
        }
        /// <summary>
        /// 创建Set方法。
        /// </summary>
        /// <param name="attrs">属性。</param>
        /// <returns></returns>
        public MethodEmitter CreateSetMethod(MethodAttributes attrs)
        {
            if (SetMethod != null)
            {
                throw new InvalidOperationException("A get method exists");
            }

            SetMethod = new MethodEmitter("Set" + Name, attrs, typeof(void));

            int i = 0;

            if (ParameterTypes != null)
            {
                Array.ForEach(ParameterTypes, type =>
                {
                    GetMethod.DefineParameter(type, ParameterAttributes.None, Name + "_" + i++);
                });
            }

            SetMethod.DefineParameter(ReturnType, ParameterAttributes.None, Name + "_" + i++);

            return SetMethod;
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="builder">类型构造器。</param>
        public void Emit(TypeBuilder builder)
        {
            var property = builder.DefineProperty(Name, Attributes, ReturnType, ParameterTypes);

            if (GetMethod != null)
            {

                property.SetGetMethod(builder.DefineMethod(GetMethod.Name, GetMethod.Attributes, GetMethod.ReturnType, GetMethod.Parameters.Select(x => x.ReturnType).ToArray()));
            }

            if (SetMethod != null)
            {
                property.SetSetMethod(builder.DefineMethod(SetMethod.Name, SetMethod.Attributes, SetMethod.ReturnType, SetMethod.Parameters.Select(x => x.ReturnType).ToArray()));
            }
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="iLGen">指令。</param>
        public override void Emit(ILGenerator iLGen)
        {
            if (GetMethod is null)
            {
                throw new EmitException($"属性“{Name}”不可读!");
            }

            iLGen.Emit(OpCodes.Callvirt, GetMethod.Value);
        }
        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="iLGen">指令。</param>
        public override void Assign(ILGenerator iLGen)
        {
            if (SetMethod is null)
            {
                throw new EmitException($"属性“{Name}”不可写!");
            }

            iLGen.Emit(OpCodes.Call, SetMethod.Value);
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="iLGen">指令。</param>
        /// <param name="value">值。</param>
        protected override void AssignCore(ILGenerator iLGen, Expression value)
        {
            if (SetMethod is null)
            {
                throw new EmitException($"属性“{Name}”不可写!");
            }

            value.Emit(iLGen);

            iLGen.Emit(OpCodes.Call, SetMethod.Value);
        }
    }
}

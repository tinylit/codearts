using CodeArts.Emit.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 字段。
    /// </summary>
    [DebuggerDisplay("{Name} ({ReturnType})")]
    public class FieldEmitter : MemberAst
    {
        private FieldBuilder builder;
        private object defaultValue;
        private bool hasDefaultValue = false;
        private readonly bool isStatic;
        private readonly List<CustomAttributeBuilder> customAttributes = new List<CustomAttributeBuilder>();

        /// <summary>
        /// 字段。
        /// </summary>
        /// <param name="name">字段的名称。</param>
        /// <param name="returnType">字段的返回类型。</param>
        /// <param name="attributes">字段的属性。</param>
        public FieldEmitter(string name, Type returnType, FieldAttributes attributes) : this(name, returnType, attributes, (attributes & FieldAttributes.Static) == FieldAttributes.Static)
        {
        }

        private FieldEmitter(string name, Type returnType, FieldAttributes attributes, bool isStatic) : base(returnType, isStatic)
        {
            Name = name;
            Attributes = attributes;
            this.isStatic = isStatic;
        }

        /// <summary>
        /// 字段的名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 字段的属性。
        /// </summary>
        public FieldAttributes Attributes { get; }

        /// <summary>
        /// 是否可写。
        /// </summary>
        public override bool CanWrite => !isStatic && (Attributes & FieldAttributes.InitOnly) != FieldAttributes.InitOnly;

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
        /// <param name="attributeData">属性。</param>
        public void SetCustomAttribute(CustomAttributeData attributeData)
        {
            if (attributeData is null)
            {
                throw new ArgumentNullException(nameof(attributeData));
            }

            customAttributes.Add(AttributeUtil.Create(attributeData));
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
        /// 获取成员数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected override void LoadCore(ILGenerator ilg)
        {
            if (isStatic)
            {
                ilg.Emit(OpCodes.Ldsfld, builder);
            }
            else
            {
                ilg.Emit(OpCodes.Ldfld, builder);
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void AssignCore(ILGenerator ilg, AstExpression value)
        {
            value.Load(ilg);

            if (isStatic)
            {
                ilg.Emit(OpCodes.Stsfld, builder);
            }
            else
            {
                ilg.Emit(OpCodes.Stfld, builder);
            }
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="builder">属性构造器。</param>
        public void Emit(FieldBuilder builder)
        {
            this.builder = builder ?? throw new ArgumentNullException(nameof(builder));

            if (hasDefaultValue)
            {
                builder.SetConstant(defaultValue);
            }

            foreach (var item in customAttributes)
            {
                builder.SetCustomAttribute(item);
            }
        }
    }
}

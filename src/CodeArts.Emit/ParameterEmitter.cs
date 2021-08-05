using CodeArts.Emit.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 参数成员。
    /// </summary>
    [DebuggerDisplay("{RuntimeType.Name} {ParameterName}")]
    public class ParameterEmitter : ParameterAst
    {
        private object defaultValue;
        private bool hasDefaultValue = false;
        private readonly List<CustomAttributeBuilder> customAttributes = new List<CustomAttributeBuilder>();

        /// <summary>
        /// 获取一个值，该值指示 System.Type 是否由引用传递。
        /// </summary>
        public override bool IsByRef => base.IsByRef || (Attributes & ParameterAttributes.Out) == ParameterAttributes.Out || (Attributes & ParameterAttributes.Retval) == ParameterAttributes.Retval;

        /// <summary>
        /// 参数类型。
        /// </summary>
        public Type ParameterType => RuntimeType;

        /// <summary>
        /// 标记。
        /// </summary>
        public ParameterAttributes Attributes { get; }

        /// <summary>
        /// 参数名称。
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="parameterType">类型。</param>
        /// <param name="position">位置。</param>
        /// <param name="attributes">标记。</param>
        /// <param name="parameterName">名称。</param>
        public ParameterEmitter(Type parameterType, int position, ParameterAttributes attributes, string parameterName) : base(parameterType, position)
        {
            Attributes = attributes;
            ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
        }

        /// <summary>
        /// 设置默认值。
        /// </summary>
        /// <param name="defaultValue">默认值。</param>
        public void SetConstant(object defaultValue)
        {
            if (defaultValue is Missing)
            {
                return;
            }

            hasDefaultValue = true;

            this.defaultValue = EmitUtils.SetConstantOfType(defaultValue, RuntimeType);
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
        /// 发行。
        /// </summary>
        /// <param name="builder">构造器。</param>
        public virtual void Emit(ParameterBuilder builder)
        {
            if (hasDefaultValue)
            {
                try
                {
                    builder.SetConstant(defaultValue);
                }
                catch (ArgumentException)
                {
                    var parameterType = RuntimeType;
                    var parameterNonNullableType = parameterType;

                    if (defaultValue == null)
                    {
                        if (parameterType.IsValueType)
                        {
                            goto label_core;
                        }
                    }
                    else if (parameterType.IsNullable())
                    {
                        parameterNonNullableType = parameterType.GetGenericArguments()[0];

                        if (parameterNonNullableType.IsEnum || parameterNonNullableType.IsAssignableFrom(defaultValue.GetType()))
                        {
                            goto label_core;
                        }
                    }

                    try
                    {
                        builder.SetConstant(System.Convert.ChangeType(defaultValue, parameterNonNullableType, CultureInfo.InvariantCulture));

                        goto label_core;
                    }
                    catch
                    {
                        // We don't care about the error thrown by an unsuccessful type coercion.
                    }

                    throw;
                }
            }

            label_core:

            foreach (var item in customAttributes)
            {
                builder.SetCustomAttribute(item);
            }
        }
    }
}

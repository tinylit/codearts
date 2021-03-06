﻿using CodeArts.Emit.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 参数成员。
    /// </summary>
    [DebuggerDisplay("{ReturnType.Name} {ParameterName}")]
    public class ParamterEmitter : ParamterAst
    {
        private object defaultValue;
        private ParameterBuilder builder;
        private bool hasDefaultValue = false;
        private readonly List<CustomAttributeBuilder> customAttributes = new List<CustomAttributeBuilder>();

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
        public ParamterEmitter(Type parameterType, int position, ParameterAttributes attributes, string parameterName) : base(parameterType, position)
        {
            if (parameterType is null)
            {
                throw new ArgumentNullException(nameof(parameterType));
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "参数位置不能小于零。");
            }
            Attributes = attributes;
            ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
        }

        /// <summary>
        /// 参数构造器。
        /// </summary>
        public ParameterBuilder Value => builder ?? throw new NotImplementedException();

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
        /// <param name="builder">构造器。</param>
        public virtual void Emit(ParameterBuilder builder)
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

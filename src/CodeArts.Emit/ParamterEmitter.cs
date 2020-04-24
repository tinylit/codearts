using CodeArts.Emit.Expressions;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 参数成员。
    /// </summary>
    [DebuggerDisplay("{ParameterName}")]
    public class ParamterEmitter : ParamterAst
    {
        private object defaultValue;
        private ParameterBuilder builder;
        private bool hasDefaultValue = false;

        /// <summary>
        /// 标记。
        /// </summary>
        public ParameterAttributes Attributes { get; }

        /// <summary>
        /// 参数名称。
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parameterType">类型。</param>
        /// <param name="position">位置。</param>
        /// <param name="attributes">标记</param>
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
            this.defaultValue = defaultValue;

            hasDefaultValue = true;

            Type parameterType = ReturnType;

            if (defaultValue is null)
            {
                if (!parameterType.IsValueType || parameterType.IsNullable())
                {
                    return;
                }

                throw new NotSupportedException($"默认值为“null”,不能作为“{parameterType}”的默认值!");
            }

            var valueType = defaultValue.GetType();

            if (valueType == parameterType)
            {
                return;
            }

            if (parameterType.IsNullable())
            {
                parameterType = Nullable.GetUnderlyingType(parameterType);
            }

            if (valueType == parameterType)
            {
                return;
            }

            this.defaultValue = System.Convert.ChangeType(defaultValue, parameterType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="builder">构造器。</param>
        public virtual void Emit(ParameterBuilder builder)
        {
            this.builder = builder;

            if (hasDefaultValue)
            {
                builder.SetConstant(defaultValue);
            }
        }
    }
}

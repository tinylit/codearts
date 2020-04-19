using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 常量。
    /// </summary>
    [DebuggerDisplay("{value}")]
    public class ConstantExpression : Expression
    {
        private readonly object value;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">值</param>
        public ConstantExpression(object value) : this(value, null)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="type">值类型</param>
        public ConstantExpression(object value, Type type) : base(type ?? value?.GetType() ?? typeof(object))
        {
            Type valueType = value?.GetType();

            if (type is null)
            {
                type = valueType ?? typeof(object);
            }
            else if (valueType != type)
            {
                throw new EmitException($"常量类型（{valueType}）和指定类型（{type}）不是有效的常量!");
            }

            if (type.IsPrimitive || type == typeof(string) || value is null)
            {
                this.value = value;
            }
            else
            {
                throw new EmitException($"类型（{type}）不是有效的常量类型!");
            }
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="iLGen">指令</param>
        public override void Emit(ILGenerator iLGen)
        {
            if (value is null)
            {
                iLGen.Emit(OpCodes.Ldnull);
            }
            else if (value is string text)
            {
                iLGen.Emit(OpCodes.Ldstr, text);
            }
            else
            {
                var code = EmitCodes.Instance[value.GetType()];

                switch (value)
                {
                    case bool b:
                        iLGen.Emit(code, b ? 1 : 0);
                        break;
                    case float f:
                        iLGen.Emit(code, f);
                        break;
                    case double d:
                        iLGen.Emit(code, d);
                        break;
                    case long l:
                        iLGen.Emit(code, l);
                        break;
                    default:
                        iLGen.Emit(code, Convert.ToInt32(value));
                        break;
                }
            }
        }
    }
}

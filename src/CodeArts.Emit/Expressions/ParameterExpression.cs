using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 参数成员。
    /// </summary>
    [DebuggerDisplay("{ParameterName}")]
    public class ParameterExpression : AssignedExpression
    {
        private readonly ParameterInfo parameter;

        /// <summary>
        /// 参数下标。
        /// </summary>
        public int Position { get; }

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
        public ParameterExpression(Type parameterType, int position, ParameterAttributes attributes, string parameterName) : base(parameterType)
        {
            if (parameterType is null)
            {
                throw new ArgumentNullException(nameof(parameterType));
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }
            Position = position;
            Attributes = attributes;
            ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
        }

        /// <summary>
        /// 取值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Emit(ILGenerator ilg)
        {
            switch (Position)
            {
                case 0:
                    ilg.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    ilg.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    ilg.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    ilg.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    ilg.Emit(OpCodes.Ldarg_S, Position);
                    break;
            }
        }

        /// <summary>
        /// 将当前堆载顶部的数据赋值给变量。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Assign(ILGenerator ilg)
        {
            ilg.Emit(OpCodes.Starg, Position);
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void AssignCore(ILGenerator ilg, Expression value)
        {
            value.Emit(ilg);

            ilg.Emit(OpCodes.Starg, Position);
        }
    }
}

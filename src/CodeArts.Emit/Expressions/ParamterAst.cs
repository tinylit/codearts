using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 参数。
    /// </summary>
    [DebuggerDisplay("{ReturnType.Name} ({position})")]
    public class ParamterAst : AssignAstExpression
    {
        /// <summary>
        /// 参数位置。
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="paramterType">参数类型。</param>
        /// <param name="position">参数位置。</param>
        public ParamterAst(Type paramterType, int position) : base(paramterType)
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "参数位置不能小于零。");
            }

            Position = position;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="parameter">参数。</param>
        public ParamterAst(ParameterInfo parameter) : this(parameter.ParameterType, parameter.Position + 1)
        {
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Assign(ILGenerator ilg)
        {
            if (Position < byte.MaxValue)
            {
                ilg.Emit(OpCodes.Starg_S, (byte)Position);
            }
            else
            {
                ilg.Emit(OpCodes.Starg, Position);
            }
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
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
                    if (Position < byte.MaxValue)
                    {
                        ilg.Emit(OpCodes.Ldarg_S, (byte)Position);

                        break;
                    }

                    ilg.Emit(OpCodes.Ldarg, Position);
                    break;
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void AssignCore(ILGenerator ilg, AstExpression value)
        {
            Assign(ilg);

            value.Load(ilg);
        }
    }
}

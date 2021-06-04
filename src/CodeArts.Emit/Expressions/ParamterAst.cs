using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 参数。
    /// </summary>
    [DebuggerDisplay("{ReturnType.Name} ({position})")]
    public class ParamterAst : AstExpression
    {
        private readonly bool canWrite = true;

        /// <summary>
        /// 参数位置。
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// 可写。
        /// </summary>
        public override bool CanWrite => canWrite;

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
            canWrite = !IsReadOnly(parameter);
        }

        private static bool IsReadOnly(ParameterInfo parameter)
        {
            if ((parameter.Attributes & (ParameterAttributes.In | ParameterAttributes.Out)) != ParameterAttributes.In)
            {
                return false;
            }

            if (parameter.GetRequiredCustomModifiers().Any(x => x == typeof(InAttribute)))
            {
                return true;
            }

            if (parameter.GetCustomAttributes(false).Any(x => x.GetType().FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute"))
            {
                return true;
            }

            return false;
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
            if (Position < byte.MaxValue)
            {
                ilg.Emit(OpCodes.Starg_S, (byte)Position);
            }
            else
            {
                ilg.Emit(OpCodes.Starg, Position);
            }

            value.Load(ilg);
        }
    }
}

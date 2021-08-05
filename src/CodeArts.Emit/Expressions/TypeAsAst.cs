using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 类型转换。
    /// </summary>
    [DebuggerDisplay("{body} as {RuntimeType.Name}")]
    public class TypeAsAst : AstExpression
    {
        private readonly AstExpression body;


        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">成员。</param>
        /// <param name="type">类型。</param>
        public TypeAsAst(AstExpression body, Type type) : base(type)
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (RuntimeType.IsValueType)
            {
                var underlyingType = RuntimeType.IsNullable()
                    ? Nullable.GetUnderlyingType(RuntimeType)
                    : RuntimeType;

                var local = ilg.DeclareLocal(RuntimeType);

                var label = ilg.DefineLabel();
                var leave = ilg.DefineLabel();

                body.Load(ilg);

                if (body.RuntimeType.IsValueType)
                {
                    ilg.Emit(OpCodes.Box, body.RuntimeType);
                }

                ilg.Emit(OpCodes.Isinst, underlyingType);

                ilg.Emit(OpCodes.Brtrue_S, label);

                EmitUtils.EmitDefaultOfType(ilg, RuntimeType);

                ilg.Emit(OpCodes.Stloc, local);

                ilg.Emit(OpCodes.Br_S, leave);

                ilg.MarkLabel(label);

                body.Load(ilg);

                if (body.RuntimeType.IsValueType)
                {
                    ilg.Emit(OpCodes.Box, body.RuntimeType);
                }

                ilg.Emit(OpCodes.Unbox_Any, RuntimeType);

                ilg.Emit(OpCodes.Stloc, local);
                ilg.MarkLabel(leave);

                ilg.Emit(OpCodes.Ldloc, local);
            }
            else
            {
                body.Load(ilg);

                if (body.RuntimeType.IsValueType)
                {
                    ilg.Emit(OpCodes.Box, body.RuntimeType);
                }

                ilg.Emit(OpCodes.Isinst, RuntimeType);
            }
        }
    }
}

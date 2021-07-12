using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 类型转换。
    /// </summary>
    [DebuggerDisplay("({RuntimeType.Name}){body}")]
    public class ConvertAst : AstExpression
    {
        private readonly AstExpression body;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <param name="convertToType">转换类型。</param>
        public ConvertAst(AstExpression body, Type convertToType) : base(convertToType)
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            body.Load(ilg);

            Type typeFrom = body.RuntimeType;

            if (typeFrom == RuntimeType)
            {
                return;
            }

            if (RuntimeType == typeof(void))
            {
                ilg.Emit(OpCodes.Pop);

                return;
            }

            EmitUtils.EmitConvertToType(ilg, typeFrom, RuntimeType.IsByRef ? RuntimeType.GetElementType() : RuntimeType, true);
        }
    }
}

using System;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 清除返回值。
    /// </summary>
    public class VoidAst : AstExpression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public VoidAst() : base(AssignableVoidType)
        {
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            ilg.Emit(OpCodes.Nop);
        }
    }
}

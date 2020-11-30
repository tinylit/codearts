using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 清除返回值。
    /// </summary>
    [DebuggerDisplay("void")]
    public class VoidAst : AstExpression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public VoidAst() : base(typeof(void))
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

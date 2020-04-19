using System;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 清除返回值。
    /// </summary>
    public class NopExpression : Expression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public NopExpression() : base(AssignableVoidType)
        {
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="iLGen">指令。</param>
        public override void Emit(ILGenerator iLGen)
        {
            iLGen.Emit(OpCodes.Nop);
        }
    }
}

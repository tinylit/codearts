using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 空合并运算。
    /// </summary>
	[DebuggerDisplay("{left} ?? {right}")]
    public class CoalesceExpression : Expression
    {
        private readonly Expression left;
        private readonly Expression right;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public CoalesceExpression(Expression left, Expression right) : base(left.ReturnType)
        {
            this.left = left ?? throw new System.ArgumentNullException(nameof(left));
            this.right = right ?? throw new System.ArgumentNullException(nameof(right));
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="iLGen">指令</param>
        public override void Emit(ILGenerator iLGen)
        {
            left.Emit(iLGen);
            iLGen.Emit(OpCodes.Dup);
            var label = iLGen.DefineLabel();
            iLGen.Emit(OpCodes.Brtrue_S, label);
            iLGen.Emit(OpCodes.Pop);
            right.Emit(iLGen);
            iLGen.MarkLabel(label);
        }
    }
}

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
        /// <param name="ilg">指令</param>
        public override void Emit(ILGenerator ilg)
        {
            left.Emit(ilg);
            ilg.Emit(OpCodes.Dup);
            var label = ilg.DefineLabel();
            ilg.Emit(OpCodes.Brtrue_S, label);
            ilg.Emit(OpCodes.Pop);
            right.Emit(ilg);
            ilg.MarkLabel(label);
        }
    }
}

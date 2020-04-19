using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 赋值。
    /// </summary>
    [DebuggerDisplay("{left}={right}")]
    public class AssignExpression : Expression
    {
        private readonly AssignedExpression left;
        private readonly Expression right;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="left">成员</param>
        /// <param name="right">表达式</param>
        public AssignExpression(AssignedExpression left, Expression right) : base(right.ReturnType)
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
            EmitCodes.EmitLoad(ilg, left);

            left.Assign(ilg, right);
        }
    }
}

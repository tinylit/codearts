using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 赋值。
    /// </summary>
    [DebuggerDisplay("{left}={right}")]
    public class AssignAst : AstExpression
    {
        private readonly AstExpression left;
        private readonly AstExpression right;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="left">成员。</param>
        /// <param name="right">表达式。</param>
        public AssignAst(AstExpression left, AstExpression right) : base(right.ReturnType)
        {
            this.left = left ?? throw new System.ArgumentNullException(nameof(left));
            this.right = right ?? throw new System.ArgumentNullException(nameof(right));
        }
        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            left.Assign(ilg, right);
        }
    }
}

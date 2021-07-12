using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 判断。
    /// </summary>
    [DebuggerDisplay("if({test})\\{ {ifTrue} \\} else \\{ {ifFalse} \\}")]
    public class IfThenElseAst : AstExpression
    {
        private readonly AstExpression test;
        private readonly AstExpression ifTrue;
        private readonly AstExpression ifFalse;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真的代码块。</param>
        /// <param name="ifFalse">为假的代码块。</param>
        public IfThenElseAst(AstExpression test, AstExpression ifTrue, AstExpression ifFalse) : base(typeof(void))
        {
            this.test = test ?? throw new ArgumentNullException(nameof(test));

            if (test.RuntimeType == typeof(bool))
            {
                this.ifTrue = ifTrue ?? throw new ArgumentNullException(nameof(ifTrue));
                this.ifFalse = ifFalse ?? throw new ArgumentNullException(nameof(ifFalse));
            }
            else
            {
                throw new ArgumentException("不是有效的条件语句!", nameof(test));
            }
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            var label = ilg.DefineLabel();
            var leave = ilg.DefineLabel();

            test.Load(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            ifTrue.Load(ilg);

            if (ifTrue.RuntimeType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.Emit(OpCodes.Br, leave);

            ilg.MarkLabel(label);

            ifFalse.Load(ilg);

            if (ifFalse.RuntimeType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.MarkLabel(leave);
        }
    }
}

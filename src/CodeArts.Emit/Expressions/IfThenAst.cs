using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 判断。
    /// </summary>
    [DebuggerDisplay("if({testExp}){ {ifTrue} }")]
    public class IfThenAst : AstExpression
    {
        private readonly AstExpression test;
        private readonly AstExpression ifTrue;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真的代码块。</param>
        public IfThenAst(AstExpression test, AstExpression ifTrue) : this(test, ifTrue, typeof(void))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真的代码块。</param>
        /// <param name="returnType">返回类型</param>
        public IfThenAst(AstExpression test, AstExpression ifTrue, Type returnType) : base(returnType)
        {
            this.test = test ?? throw new ArgumentNullException(nameof(test));

            if (test.ReturnType == typeof(bool))
            {
                this.ifTrue = ifTrue ?? throw new ArgumentNullException(nameof(ifTrue));
            }
            else
            {
                throw new ArgumentException("不是有效的条件语句!", nameof(test));
            }

            if (!(returnType == typeof(void) || ifTrue.ReturnType == returnType || returnType.IsAssignableFrom(ifTrue.ReturnType)))
            {
                throw new AstException($"返回值“{returnType}”和表达式类型“{ifTrue.ReturnType}”无法进行转换!");
            }
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Load(ILGenerator ilg)
        {
            if (ReturnType == typeof(void))
            {
                EmitVoid(ilg, test, ifTrue);
            }
            else
            {
                Emit(ilg, test, ifTrue, ReturnType);
            }
        }

        private static void Emit(ILGenerator ilg, AstExpression test, AstExpression ifTrue, Type returnType)
        {
            var label = ilg.DefineLabel();
            var leave = ilg.DefineLabel();

            test.Load(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            ilg.Emit(OpCodes.Nop);

            if (returnType == ifTrue.ReturnType)
            {
                ifTrue.Load(ilg);
            }
            else
            {
                new ConvertAst(ifTrue, returnType).Load(ilg);
            }

            ilg.Emit(OpCodes.Br_S, leave);

            ilg.MarkLabel(label);

            new DefaultAst(returnType).Load(ilg);

            ilg.MarkLabel(leave);
        }

        private static void EmitVoid(ILGenerator ilg, AstExpression test, AstExpression ifTrue)
        {
            var label = ilg.DefineLabel();

            test.Load(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            ifTrue.Load(ilg);

            if (ifTrue.ReturnType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.MarkLabel(label);
        }
    }
}

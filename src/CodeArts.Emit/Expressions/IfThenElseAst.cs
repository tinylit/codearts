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
        public IfThenElseAst(AstExpression test, AstExpression ifTrue, AstExpression ifFalse) : base(AnalysisReturnType(ifTrue, ifFalse))
        {
            this.test = test ?? throw new ArgumentNullException(nameof(test));

            if (test.ReturnType == typeof(bool))
            {
                this.ifTrue = ifTrue ?? throw new ArgumentNullException(nameof(ifTrue));
                this.ifFalse = ifFalse ?? throw new ArgumentNullException(nameof(ifFalse));
            }
            else
            {
                throw new ArgumentException("不是有效的条件语句!", nameof(test));
            }
        }

        private static Type AnalysisReturnType(AstExpression ifTrue, AstExpression ifFalse)
        {
            if (ifTrue is null)
            {
                throw new ArgumentNullException(nameof(ifTrue));
            }

            if (ifFalse is null)
            {
                throw new ArgumentNullException(nameof(ifFalse));
            }

            if (ifTrue.ReturnType == typeof(void) || ifFalse.ReturnType == typeof(void))
            {
                return typeof(void);
            }
            if (ifTrue.ReturnType == ifFalse.ReturnType)
            {
                return ifTrue.ReturnType;
            }

            if (ifTrue.ReturnType.IsAssignableFrom(ifFalse.ReturnType))
            {
                return ifTrue.ReturnType;
            }

            if (ifTrue.ReturnType.IsSubclassOf(ifFalse.ReturnType))
            {
                return ifFalse.ReturnType;
            }

            return typeof(void);
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Load(ILGenerator ilg)
        {
            if (ReturnType == typeof(void))
            {
                EmitVoid(ilg, test, ifTrue, ifFalse);
            }
            else
            {
                Emit(ilg, test, ifTrue, ifFalse, ReturnType);
            }
        }

        private static void Emit(ILGenerator ilg, AstExpression test, AstExpression ifTrue, AstExpression ifFalse, Type returnType)
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

            if (returnType == ifFalse.ReturnType)
            {
                ifFalse.Load(ilg);
            }
            else
            {
                new ConvertAst(ifFalse, returnType).Load(ilg);
            }

            ilg.MarkLabel(leave);
        }

        private static void EmitVoid(ILGenerator ilg, AstExpression test, AstExpression ifTrue, AstExpression ifFalse)
        {
            var label = ilg.DefineLabel();
            var leave = ilg.DefineLabel();

            test.Load(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            ifTrue.Load(ilg);

            if (ifTrue.ReturnType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.Emit(OpCodes.Br, leave);

            ilg.MarkLabel(label);

            ifFalse.Load(ilg);

            if (ifFalse.ReturnType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.MarkLabel(leave);
        }
    }
}

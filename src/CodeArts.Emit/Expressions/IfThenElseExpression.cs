using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 判断。
    /// </summary>
    [DebuggerDisplay("if({testExp}){ {ifTrue} } else { {ifFalse} }")]
    public class IfThenElseExpression : Expression
    {
        private readonly Expression test;
        private readonly Expression ifTrue;
        private readonly Expression ifFalse;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真的代码块。</param>
        /// <param name="ifFalse">为假的代码块。</param>
        public IfThenElseExpression(Expression test, Expression ifTrue, Expression ifFalse) : base(AnalysisReturnType(ifTrue, ifFalse))
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

        private static Type AnalysisReturnType(Expression ifTrue, Expression ifFalse)
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
        /// <param name="iLGen">指令</param>
        public override void Emit(ILGenerator iLGen)
        {
            if (ReturnType == typeof(void))
            {
                EmitVoid(iLGen, test, ifTrue, ifFalse);
            }
            else
            {
                Emit(iLGen, test, ifTrue, ifFalse, ReturnType);
            }
        }

        private static void Emit(ILGenerator iLGen, Expression test, Expression ifTrue, Expression ifFalse, Type returnType)
        {
            var label = iLGen.DefineLabel();
            var leave = iLGen.DefineLabel();

            test.Emit(iLGen);

            iLGen.Emit(OpCodes.Brfalse_S, label);

            if (returnType == ifTrue.ReturnType)
            {
                ifTrue.Emit(iLGen);
            }
            else
            {
                new ConvertExpression(ifTrue, returnType).Emit(iLGen);
            }

            iLGen.Emit(OpCodes.Br, leave);

            iLGen.MarkLabel(label);

            if (returnType == ifFalse.ReturnType)
            {
                ifFalse.Emit(iLGen);
            }
            else
            {
                new ConvertExpression(ifFalse, returnType).Emit(iLGen);
            }

            iLGen.MarkLabel(leave);
        }

        private static void EmitVoid(ILGenerator iLGen, Expression test, Expression ifTrue, Expression ifFalse)
        {
            var label = iLGen.DefineLabel();
            var leave = iLGen.DefineLabel();

            test.Emit(iLGen);

            iLGen.Emit(OpCodes.Brfalse_S, label);

            ifTrue.Emit(iLGen);

            if (ifTrue.ReturnType != typeof(void))
            {
                iLGen.Emit(OpCodes.Pop);
            }

            iLGen.Emit(OpCodes.Br, leave);

            iLGen.MarkLabel(label);

            ifFalse.Emit(iLGen);

            if (ifFalse.ReturnType != typeof(void))
            {
                iLGen.Emit(OpCodes.Pop);
            }

            iLGen.MarkLabel(leave);
        }
    }
}

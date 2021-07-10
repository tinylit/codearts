using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 判断。
    /// </summary>
    [DebuggerDisplay("{test} ? {ifTrue} : {ifFalse}")]
    public class ConditionAst : AstExpression
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
        public ConditionAst(AstExpression test, AstExpression ifTrue, AstExpression ifFalse) : this(test, ifTrue, ifFalse, AnalysisReturnType(ifTrue, ifFalse))
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真的代码块。</param>
        /// <param name="ifFalse">为假的代码块。</param>
        /// <param name="returnType">结果类型。</param>
        public ConditionAst(AstExpression test, AstExpression ifTrue, AstExpression ifFalse, Type returnType) : base(returnType)
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

            if (returnType == typeof(void))
            {
                return;
            }

            if (returnType == ifTrue.ReturnType)
            {

            }
            else if (returnType.IsAssignableFrom(ifTrue.ReturnType))
            {
                this.ifTrue = new ConvertAst(ifTrue, returnType);
            }
            else
            {
                throw new ArgumentException($"表达式类型“{ifTrue.ReturnType}”不能默认转换为“{returnType}”!", nameof(ifTrue));
            }

            if (returnType == ifFalse.ReturnType)
            {

            }
            else if (returnType.IsAssignableFrom(ifTrue.ReturnType))
            {
                this.ifFalse = new ConvertAst(ifFalse, returnType);
            }
            else
            {
                throw new ArgumentException($"表达式类型“{ifFalse.ReturnType}”不能默认转换为“{returnType}”!", nameof(ifFalse));
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
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (ReturnType == typeof(void))
            {
                EmitVoid(ilg);
            }
            else
            {
                Emit(ilg);
            }
        }

        private void Emit(ILGenerator ilg)
        {
            var label = ilg.DefineLabel();
            var leave = ilg.DefineLabel();
            var variable = ilg.DeclareLocal(ReturnType);

            test.Load(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            ilg.Emit(OpCodes.Nop);

            ifTrue.Load(ilg);

            ilg.Emit(OpCodes.Stloc, variable);

            ilg.Emit(OpCodes.Leave_S, leave);

            ilg.MarkLabel(label);

            ifFalse.Load(ilg);

            ilg.Emit(OpCodes.Stloc, variable);

            ilg.MarkLabel(leave);

            ilg.Emit(OpCodes.Ldloc, variable);
        }

        private void EmitVoid(ILGenerator ilg)
        {
            var label = ilg.DefineLabel();
            var leave = ilg.DefineLabel();

            test.Load(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            ilg.Emit(OpCodes.Nop);

            ifTrue.Load(ilg);

            if (ifFalse.ReturnType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.MarkLabel(label);

            ilg.Emit(OpCodes.Leave_S, leave);

            ilg.MarkLabel(label);

            ifFalse.Load(ilg);

            if (ifTrue.ReturnType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.MarkLabel(leave);
        }
    }
}

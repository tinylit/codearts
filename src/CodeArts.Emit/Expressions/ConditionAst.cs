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

            if (test.RuntimeType == typeof(bool))
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

            if (returnType == ifTrue.RuntimeType)
            {

            }
            else if (returnType.IsAssignableFrom(ifTrue.RuntimeType))
            {
                this.ifTrue = new ConvertAst(ifTrue, returnType);
            }
            else
            {
                throw new ArgumentException($"表达式类型“{ifTrue.RuntimeType}”不能默认转换为“{returnType}”!", nameof(ifTrue));
            }

            if (returnType == ifFalse.RuntimeType)
            {

            }
            else if (returnType.IsAssignableFrom(ifTrue.RuntimeType))
            {
                this.ifFalse = new ConvertAst(ifFalse, returnType);
            }
            else
            {
                throw new ArgumentException($"表达式类型“{ifFalse.RuntimeType}”不能默认转换为“{returnType}”!", nameof(ifFalse));
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

            if (ifTrue.RuntimeType == typeof(void) || ifFalse.RuntimeType == typeof(void))
            {
                return typeof(void);
            }

            if (ifTrue.RuntimeType == ifFalse.RuntimeType)
            {
                return ifTrue.RuntimeType;
            }

            if (ifTrue.RuntimeType.IsAssignableFrom(ifFalse.RuntimeType))
            {
                return ifTrue.RuntimeType;
            }

            if (ifTrue.RuntimeType.IsSubclassOf(ifFalse.RuntimeType))
            {
                return ifFalse.RuntimeType;
            }

            return typeof(void);
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (RuntimeType == typeof(void))
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
            var variable = ilg.DeclareLocal(RuntimeType);

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

            if (ifFalse.RuntimeType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.MarkLabel(label);

            ilg.Emit(OpCodes.Leave_S, leave);

            ilg.MarkLabel(label);

            ifFalse.Load(ilg);

            if (ifTrue.RuntimeType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.MarkLabel(leave);
        }
    }
}

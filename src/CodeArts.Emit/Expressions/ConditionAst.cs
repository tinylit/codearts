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
        public ConditionAst(AstExpression test, AstExpression ifTrue, AstExpression ifFalse) : base(AnalysisReturnType(ifTrue, ifFalse))
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
                throw new NotSupportedException("不支持无返回值类型!");
            }

            if (!returnType.IsAssignableFrom(ifTrue.ReturnType))
            {
                throw new ArgumentException($"表达式类型“{ifTrue.ReturnType}”不能默认转换为“{returnType}”!", nameof(ifTrue));
            }

            if (!returnType.IsAssignableFrom(ifFalse.ReturnType))
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
                throw new NotSupportedException("不支持无返回值类型!");
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

            throw new NotSupportedException("不能进行类型转换!");
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
                Emit(ilg, ReturnType);
            }
        }

        private void Emit(ILGenerator ilg, Type returnType)
        {
            var label = ilg.DefineLabel();
            var leave = ilg.DefineLabel();

            test.Load(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            if (returnType == ifTrue.ReturnType)
            {
                ifTrue.Load(ilg);
            }
            else
            {
                new ConvertAst(ifTrue, returnType)
                    .Load(ilg);
            }

            ilg.Emit(OpCodes.Br_S, leave);

            ilg.MarkLabel(label);

            if (returnType == ifFalse.ReturnType)
            {
                ifFalse.Load(ilg);
            }
            else
            {
                new ConvertAst(ifFalse, returnType)
                    .Load(ilg);
            }

            ilg.MarkLabel(leave);
        }

        private void EmitVoid(ILGenerator ilg)
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

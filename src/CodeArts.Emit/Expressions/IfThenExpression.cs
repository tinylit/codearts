using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 判断。
    /// </summary>
    [DebuggerDisplay("if({testExp}){ {ifTrue} }")]
    public class IfThenExpression : Expression
    {
        private readonly Expression test;
        private readonly Expression ifTrue;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真的代码块。</param>
        public IfThenExpression(Expression test, Expression ifTrue) : this(test, ifTrue, typeof(void))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真的代码块。</param>
        /// <param name="returnType">返回类型</param>
        public IfThenExpression(Expression test, Expression ifTrue, Type returnType) : base(returnType)
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
                throw new EmitException($"返回值“{returnType}”和表达式类型“{ifTrue.ReturnType}”无法进行转换!");
            }
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="iLGen">指令</param>
        public override void Emit(ILGenerator iLGen)
        {
            if (ReturnType == typeof(void))
            {
                EmitVoid(iLGen, test, ifTrue);
            }
            else
            {
                Emit(iLGen, test, ifTrue, ReturnType);
            }
        }

        private static void Emit(ILGenerator iLGen, Expression test, Expression ifTrue, Type returnType)
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

            new DefaultExpression(returnType).Emit(iLGen);

            iLGen.MarkLabel(leave);
        }

        private static void EmitVoid(ILGenerator iLGen, Expression test, Expression ifTrue)
        {
            var label = iLGen.DefineLabel();

            test.Emit(iLGen);

            iLGen.Emit(OpCodes.Brfalse_S, label);

            ifTrue.Emit(iLGen);

            if (ifTrue.ReturnType != typeof(void))
            {
                iLGen.Emit(OpCodes.Pop);
            }

            iLGen.MarkLabel(label);
        }
    }
}

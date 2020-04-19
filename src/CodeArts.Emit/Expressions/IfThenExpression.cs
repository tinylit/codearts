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
        /// <param name="ilg">指令</param>
        public override void Emit(ILGenerator ilg)
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

        private static void Emit(ILGenerator ilg, Expression test, Expression ifTrue, Type returnType)
        {
            var label = ilg.DefineLabel();
            var leave = ilg.DefineLabel();

            test.Emit(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            if (returnType == ifTrue.ReturnType)
            {
                ifTrue.Emit(ilg);
            }
            else
            {
                new ConvertExpression(ifTrue, returnType).Emit(ilg);
            }

            ilg.Emit(OpCodes.Br, leave);

            ilg.MarkLabel(label);

            new DefaultExpression(returnType).Emit(ilg);

            ilg.MarkLabel(leave);
        }

        private static void EmitVoid(ILGenerator ilg, Expression test, Expression ifTrue)
        {
            var label = ilg.DefineLabel();

            test.Emit(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            ifTrue.Emit(ilg);

            if (ifTrue.ReturnType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.MarkLabel(label);
        }
    }
}

﻿using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 结束。
    /// </summary>
    [DebuggerDisplay("finally{ {body} }")]
    public class FinallyExpression : Expression
    {
        private readonly Expression body;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">表达式</param>
        public FinallyExpression(Expression body) : base(typeof(void))
        {
            this.body = body ?? throw new System.ArgumentNullException(nameof(body));
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Emit(ILGenerator ilg)
        {
            ilg.BeginFinallyBlock();

            body.Emit(ilg);

            if (body.ReturnType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }
        }
    }
}

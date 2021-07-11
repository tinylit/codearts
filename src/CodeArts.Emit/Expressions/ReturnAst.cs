using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 返回。
    /// </summary>
    [DebuggerDisplay("return {ReturnType.Name}")]
    public class ReturnAst : AstExpression
    {
        private readonly AstExpression body;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ReturnAst() : base(typeof(void)) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">返回结果的表达式。</param>
        public ReturnAst(AstExpression body) : base(body.ReturnType)
        {
            this.body = body;

            if (body.ReturnType == typeof(void))
            {
                throw new ArgumentException("不能返回无返回值类型!", nameof(body));
            }
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (body is null)
            {
                ilg.Emit(OpCodes.Nop);
            }
            else
            {
                body.Load(ilg);
            }

            ilg.Emit(OpCodes.Ret);
        }
    }
}

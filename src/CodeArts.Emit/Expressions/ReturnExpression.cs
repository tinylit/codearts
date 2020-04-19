using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 返回。
    /// </summary>
    [DebuggerDisplay("return {body}")]
    public class ReturnExpression : Expression
    {
        private readonly Expression body;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ReturnExpression() : base(typeof(void)) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">返回结果的表达式。</param>
        public ReturnExpression(Expression body) : base(body.ReturnType) => this.body = body;

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Emit(ILGenerator ilg)
        {
            body?.Emit(ilg);

            ilg.Emit(OpCodes.Ret);
        }
    }
}

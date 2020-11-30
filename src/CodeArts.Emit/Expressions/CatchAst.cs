using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 捕获异常。
    /// </summary>
    [DebuggerDisplay("catch({variable}){ {body} }")]
    public class CatchAst : AstExpression
    {
        private readonly AstExpression body;
        private readonly Type exceptionType;
        private readonly VariableAst variable;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">代码块。</param>
        public CatchAst(AstExpression body) : this(body, typeof(Exception)) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">代码块。</param>
        /// <param name="exceptionType">异常类型。</param>
        public CatchAst(AstExpression body, Type exceptionType) : this(body, exceptionType, null)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">代码块。</param>
        /// <param name="variable">变量。</param>
        public CatchAst(AstExpression body, VariableAst variable) : this(body, typeof(Exception), variable)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">代码块。</param>
        /// <param name="exceptionType">异常类型。</param>
        /// <param name="variable">变量。</param>
        public CatchAst(AstExpression body, Type exceptionType, VariableAst variable) : base(body.ReturnType)
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));
            this.exceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
            this.variable = variable;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            ilg.BeginCatchBlock(exceptionType);

            variable?.Assign(ilg);

            body.Load(ilg);
        }
    }
}

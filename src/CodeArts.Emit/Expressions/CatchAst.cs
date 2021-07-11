using System;
using System.Collections.Generic;
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
        private class CatchBlockAst : AstExpression
        {
            public CatchBlockAst(Type returnType) : base(returnType)
            {
            }

            public override void Load(ILGenerator ilg)
            {
                ilg.BeginCatchBlock(ReturnType);
            }
        }

        private readonly AstExpression body;
        private readonly Type exceptionType;
        private readonly VariableAst variable;

        private readonly List<AstExpression> codes = new List<AstExpression>();
        private readonly List<VariableAst> variables = new List<VariableAst>();

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

            if (body is ReturnAst || body is BlockAst blockAst && blockAst.HasReturn)
            {
                throw new AstException("捕获异常的表达式会将结果推到堆上，不能写返回！");
            }

            this.exceptionType = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
            this.variable = variable;
        }

        /// <summary>
        /// 声明变量。
        /// </summary>
        /// <param name="variableType">变量类型。</param>
        /// <returns></returns>
        public VariableAst DeclareVariable(Type variableType)
        {
            if (variableType is null)
            {
                throw new ArgumentNullException(nameof(variableType));
            }

            var variable = new VariableAst(variableType);
            variables.Add(variable);
            return variable;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (variable is null)
            {
                ilg.BeginCatchBlock(exceptionType);
            }
            else
            {
                variable.Assign(ilg, new CatchBlockAst(exceptionType));
            }

            ilg.Emit(OpCodes.Nop);

            body.Load(ilg);
        }
    }
}

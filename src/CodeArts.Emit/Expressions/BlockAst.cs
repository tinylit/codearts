using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 代码块。
    /// </summary>
    public class BlockAst : AstExpression
    {
        private readonly List<AstExpression> codes = new List<AstExpression>();
        private readonly List<VariableAst> variables = new List<VariableAst>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        public BlockAst(Type returnType) : base(returnType)
        {
        }

        /// <summary>
        /// 是否为空。
        /// </summary>
        public bool IsEmpty => codes.Count == 0;

        /// <summary>
        /// 声明变量。
        /// </summary>
        /// <param name="variableType">变量类型</param>
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
        /// 添加代码。
        /// </summary>
        /// <param name="code">代码。</param>
        /// <returns></returns>
        public BlockAst Append(AstExpression code)
        {
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            codes.Add(code);

            return this;
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            Type returnType = ReturnType;

            foreach (var variable in variables)
            {
                variable.Declare(ilg);
            }

            AstExpression astCode = null;

            foreach (var code in codes)
            {
                if (code is ReturnAst returnAst)
                {
                    if (returnType != returnAst.ReturnType && returnAst.ReturnType != AssignableVoidType)
                    {
                        throw new AstException($"返回类型“{returnAst.ReturnType}”和预期的返回类型“{returnType}”不相同!");
                    }
                }

                astCode = code;

                code.Load(ilg);
            }

            if (astCode is ReturnAst)
            {
                return;
            }

            if (returnType == typeof(void))
            {
                ilg.Emit(OpCodes.Nop);
            }

            ilg.Emit(OpCodes.Ret);
        }
    }
}

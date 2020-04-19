using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 代码块。
    /// </summary>
    public class BlockExpression : Expression
    {
        private readonly List<Expression> codes = new List<Expression>();
        private readonly List<VariableExpression> variables = new List<VariableExpression>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        public BlockExpression(Type returnType) : base(returnType)
        {
        }

        /// <summary>
        /// 是否为空。
        /// </summary>
        public bool IsEmpty => variables.Count == 0 && codes.Count == 0;

        /// <summary>
        /// 声明变量。
        /// </summary>
        /// <param name="variableType">变量类型</param>
        /// <returns></returns>
        public VariableExpression DeclareVariable(Type variableType)
        {
            if (variableType is null)
            {
                throw new ArgumentNullException(nameof(variableType));
            }

            var variable = new VariableExpression(variableType);
            variables.Add(variable);
            return variable;
        }

        /// <summary>
        /// 添加代码。
        /// </summary>
        /// <param name="code">代码。</param>
        /// <returns></returns>
        public BlockExpression Append(Expression code)
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
        public override void Emit(ILGenerator ilg)
        {
            Type returnType = ReturnType;

            foreach (var variable in variables)
            {
                variable.Declare(ilg);
            }

            foreach (var code in codes)
            {
                code.Emit(ilg);

                returnType = code.ReturnType;
            }

            if (ReturnType == typeof(void))
            {
                if (returnType != typeof(void))
                {
                    ilg.Emit(OpCodes.Nop);
                }

                ilg.Emit(OpCodes.Ret);

                return;
            }

            if (returnType == ReturnType)
            {
                ilg.Emit(OpCodes.Ret);

                return;
            }

            if (ReturnType.IsAssignableFrom(returnType))
            {
                ilg.Emit(OpCodes.Isinst, ReturnType);

                ilg.Emit(OpCodes.Ret);

                return;
            }

            throw new EmitException($"返回类型“{returnType}”和预期定义的返回类型“{ReturnType}”无法进行默认转换!");
        }
    }
}

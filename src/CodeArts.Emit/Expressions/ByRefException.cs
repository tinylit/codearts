using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// ByRef
    /// </summary>
    [DebuggerDisplay("ref {variable}")]
    public class ByRefException : Expression
    {
        private readonly VariableExpression variable;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="variable">变量</param>
        public ByRefException(VariableExpression variable) : base(variable.ReturnType)
        {
            this.variable = variable ?? throw new ArgumentNullException(nameof(variable));
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Emit(ILGenerator ilg) => variable.Emit(ilg);
    }
}

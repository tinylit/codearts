using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 常量。
    /// </summary>
    [DebuggerDisplay("{value}")]
    public class ConstantAst : AstExpression
    {
        private readonly object value;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">值</param>
        public ConstantAst(object value) : this(value, null)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">值</param>
        /// <param name="type">值类型</param>
        public ConstantAst(object value, Type type) : base(type ?? value?.GetType() ?? typeof(object))
        {
            this.value = value;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Load(ILGenerator ilg) => EmitCodes.EmitConstantOfType(ilg, value, ReturnType);
    }
}

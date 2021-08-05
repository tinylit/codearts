using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 默认值。
    /// </summary>
    [DebuggerDisplay("default({RuntimeType.Name})")]
    public class DefaultAst : AstExpression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="defaultType">类型。</param>
        public DefaultAst(Type defaultType) : base(defaultType)
        {
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg) => EmitUtils.EmitDefaultOfType(ilg, RuntimeType);
    }
}

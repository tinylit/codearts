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
        /// <param name="value">值。</param>
        public ConstantAst(object value) : this(value, null)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="type">值类型。</param>
        public ConstantAst(object value, Type type) : base(type ?? value?.GetType() ?? typeof(object))
        {
            if (value is null || value.GetType() == ReturnType || ReturnType.IsAssignableFrom(value.GetType()))
            {
                this.value = value;
            }
            else
            {
                throw new NotSupportedException($"常量值类型({value.GetType()})和指定类型({ReturnType})无法进行转换!");
            }
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg) => EmitUtils.EmitConstantOfType(ilg, value, ReturnType);
    }
}

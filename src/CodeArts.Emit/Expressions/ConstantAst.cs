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
        public ConstantAst(object value) : this(value, value is MethodInfo ? typeof(MethodInfo) : value is Type ? typeof(Type) : value?.GetType() ?? typeof(object))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="type">值类型。</param>
        public ConstantAst(object value, Type type) : base(type)
        {
            if (value is null)
            {
                if (type.IsValueType && !type.IsNullable())
                {
                    throw new NotSupportedException($"常量null，不能对值类型({type})进行转换!");
                }

                this.value = value;
            }
            else if (value is Type returnType ? returnType == type : value.GetType() == type || type.IsAssignableFrom(value.GetType()))
            {
                this.value = value;
            }
            else
            {
                throw new NotSupportedException($"常量值类型({value.GetType()})和指定类型({type})无法进行转换!");
            }
        }

        /// <summary>
        /// 空的。
        /// </summary>
        public bool IsNull => value is null;

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg) => EmitUtils.EmitConstantOfType(ilg, value, RuntimeType);
    }
}

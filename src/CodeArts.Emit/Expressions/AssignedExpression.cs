using System;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 被赋值的表达式。
    /// </summary>
    public abstract class AssignedExpression : Expression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="type">类型</param>
        protected AssignedExpression(Type type) : base(type) { }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public virtual void Assign(ILGenerator ilg) => throw new NotImplementedException();

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        public void Assign(ILGenerator ilg, Expression value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.ReturnType == typeof(void))
            {
                throw new EmitException("无返回值类型赋值不能用于赋值运算!");
            }

            AssignCore(ilg, value);
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected abstract void AssignCore(ILGenerator ilg, Expression value);
    }
}

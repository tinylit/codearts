using System;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 被赋值的表达式。
    /// </summary>
    public abstract class AssignAstExpression : AstExpression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="type">类型。</param>
        protected AssignAstExpression(Type type) : base(type) { }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public abstract void Assign(ILGenerator ilg);

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        public void Assign(ILGenerator ilg, AstExpression value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.ReturnType == typeof(void))
            {
                throw new AstException("无返回值类型赋值不能用于赋值运算!");
            }

            AssignCore(ilg, value);
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected abstract void AssignCore(ILGenerator ilg, AstExpression value);
    }
}

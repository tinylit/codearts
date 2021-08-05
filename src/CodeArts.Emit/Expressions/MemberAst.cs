using System;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 成员。
    /// </summary>
    public abstract class MemberAst : AstExpression
    {
        /// <summary>
        /// 引用。
        /// </summary>
        public AstExpression Expression { set; get; } = This;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        protected MemberAst(Type returnType) : base(returnType) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        /// <param name="isStatic">是静态成员。</param>
        protected MemberAst(Type returnType, bool isStatic) : base(returnType)
        {
            if (isStatic)
            {
                Expression = null;
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        /// <param name="reference">引用。</param>
        protected MemberAst(Type returnType, AstExpression reference) : base(returnType) => Expression = reference;

        /// <summary>
        /// 取值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public sealed override void Load(ILGenerator ilg)
        {
            Expression?.Load(ilg);

            LoadCore(ilg);
        }

        /// <summary>
        /// 获取成员数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected abstract void LoadCore(ILGenerator ilg);

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected virtual void AssignCore(ILGenerator ilg, AstExpression value) => throw new NotImplementedException();

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected sealed override void Assign(ILGenerator ilg, AstExpression value)
        {
            Expression?.Load(ilg);

            AssignCore(ilg, value);
        }
    }
}

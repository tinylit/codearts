using System;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 成员。
    /// </summary>
    public abstract class MemberAst : AssignAstExpression
    {
        private static class AnyAssignable { }

        /// <summary>
        /// 暂不确定的类型。
        /// </summary>
        private static readonly Type AnyAssignableType = typeof(AnyAssignable);

        /// <summary>
        /// 当前上下文对象。
        /// </summary>
        private static readonly ThisAst Instance = new ThisAst(AnyAssignableType);

        /// <summary>
        /// 引用。
        /// </summary>
        public AstExpression Expression { set; get; } = Instance;

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

            MemberLoad(ilg);
        }

        /// <summary>
        /// 获取成员数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected abstract void MemberLoad(ILGenerator ilg);
    }
}

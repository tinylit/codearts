using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 成员。
    /// </summary>
    public abstract class MemberExpression : AssignedExpression
    {
        /// <summary>
        /// 成员本身。
        /// </summary>
        [DebuggerDisplay("this")]
        private class ThisExpression : Expression
        {
            /// <summary>
            /// 当前上下文对象。
            /// </summary>
            public static readonly ThisExpression Instance = new ThisExpression();

            private ThisExpression() : base(AssignableVoidType)
            {
            }

            /// <summary>
            /// 加载成员数据。
            /// </summary>
            /// <param name="ilg">指令。</param>
            public override void Emit(ILGenerator ilg)
            {
                ilg.Emit(OpCodes.Ldarg_0);
            }
        }

        /// <summary>
        /// 引用。
        /// </summary>
        public Expression Expression { private set; get; } = ThisExpression.Instance;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        protected MemberExpression(Type returnType) : base(returnType) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        /// <param name="isStatic">是静态成员</param>
        protected MemberExpression(Type returnType, bool isStatic) : base(returnType)
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
        protected MemberExpression(Type returnType, Expression reference) : base(returnType) => Expression = reference;

        /// <summary>
        /// 取值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Emit(ILGenerator ilg) => EmitCodes.EmitLoad(ilg, this);
    }
}

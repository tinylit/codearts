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
        public AstExpression Expression { set; get; } = ThisAst.Instance;

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
        public sealed override void Assign(ILGenerator ilg, AstExpression value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var returnType = ReturnType;

            if (returnType == typeof(void))
            {
                throw new AstException("不能对无返回值类型进行赋值运算!");
            }

            if (value is ThisAst)
            {
                goto label_core;
            }

            var valueType = value.ReturnType;

            if (valueType == typeof(void))
            {
                throw new AstException("无返回值类型赋值不能用于赋值运算!");
            }

            if (valueType != returnType && !returnType.IsAssignableFrom(valueType) && (valueType.IsByRef ? valueType.GetElementType() : valueType) != (returnType.IsByRef ? returnType.GetElementType() : returnType))
            {
                throw new AstException("值表达式类型和当前表达式类型不相同!");
            }

            label_core:

            if (CanWrite)
            {
                Expression?.Load(ilg);

                AssignCore(ilg, value);
            }
            else
            {
                throw new AstException("当前表达式不可写!");
            }
        }
    }
}

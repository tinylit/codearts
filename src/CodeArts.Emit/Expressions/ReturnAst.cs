using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 返回。
    /// </summary>
    [DebuggerDisplay("return {body}")]
    public class ReturnAst : AstExpression
    {
        private sealed class AnyDynamic { }

        /// <summary>
        /// 任意动态类型。
        /// </summary>
        private static readonly Type AnyDynamicType = typeof(AnyDynamic);

        private readonly AstExpression body;

        /// <summary>
        /// 无返回值（自动清除栈顶数据）。
        /// </summary>
        public static readonly ReturnAst Void = new ReturnAst(typeof(void));

        private ReturnAst(Type returnType) : base(returnType) => IsEmpty = true;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public ReturnAst() : base(AnyDynamicType) => IsEmpty = true;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">返回结果的表达式。</param>
        public ReturnAst(AstExpression body) : base(body?.RuntimeType ?? throw new ArgumentNullException(nameof(body)))
        {
            if (body is ReturnAst returnAst)
            {
                if (returnAst.IsEmpty)
                {
                    IsEmpty = true;
                }
                else
                {
                    this.body = returnAst.body;
                }
            }
            else
            {
                this.body = body;

                if (body.RuntimeType == typeof(void))
                {
                    throw new ArgumentException("不能返回无返回值类型!", nameof(body));
                }
            }
        }

        /// <summary>
        /// 未设置返回结果。
        /// </summary>
        public bool IsEmpty { get; }

        /// <summary>
        /// 拆箱。
        /// </summary>
        /// <exception cref="AstException"><see cref="IsEmpty"/>为true时，无任何数据异常。</exception>
        /// <returns>结果表达式。</returns>
        public AstExpression Unbox()
        {
            if (IsEmpty)
            {
                throw new AstException("无任何数据!");
            }

            return body;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (!IsEmpty)
            {
                body.Load(ilg);
            }
            else if (RuntimeType == typeof(void))
            {
                ilg.Emit(OpCodes.Nop);
            }

            ilg.Emit(OpCodes.Ret);
        }
    }
}

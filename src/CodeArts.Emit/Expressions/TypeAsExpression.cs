using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 类型转换。
    /// </summary>
    [DebuggerDisplay("{body} as {Type}")]
    public class TypeAsExpression : Expression
    {
        private readonly Expression body;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">成员</param>
        /// <param name="type">类型</param>
        public TypeAsExpression(Expression body, Type type) : base(type)
        {
            this.body = body;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Emit(ILGenerator ilg)
        {
            body.Emit(ilg);

            ilg.Emit(OpCodes.Isinst, ReturnType);
        }
    }
}

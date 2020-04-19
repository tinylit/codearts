using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 抛出异常。
    /// </summary>
    [DebuggerDisplay("throw new {exceptionType.Name}({errorMsg})")]
    public class ThrowExpression : Expression
    {
        private readonly string errorMsg;
        private readonly bool hasErrorMsg = false;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="exceptionType">异常类型。</param>
        public ThrowExpression(Type exceptionType) : base(exceptionType)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="exceptionType">异常类型。</param>
        /// <param name="errorMsg">异常消息</param>
        public ThrowExpression(Type exceptionType, string errorMsg) : base(exceptionType)
        {
            hasErrorMsg = true;
            this.errorMsg = errorMsg;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="iLGen">指令</param>
        public override void Emit(ILGenerator iLGen)
        {
            NewExpression exception = hasErrorMsg ? new NewExpression(ReturnType, new ConstantExpression(errorMsg)) : new NewExpression(ReturnType);

            exception.Emit(iLGen);

            iLGen.Emit(OpCodes.Throw);
        }
    }
}

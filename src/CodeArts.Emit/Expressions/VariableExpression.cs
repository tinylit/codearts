using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 变量。
    /// </summary>
    [DebuggerDisplay("{returnType.Name} variable")]
    public sealed class VariableExpression : AssignedExpression
    {
        private LocalBuilder local;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">类型</param>
        public VariableExpression(Type returnType) : base(returnType)
        {
        }

        /// <summary>
        /// 声明。
        /// </summary>
        /// <param name="ilg">指令。</param>
        internal void Declare(ILGenerator ilg)
        {
            if(local is null)
            {
                local = ilg.DeclareLocal(ReturnType);
            }
        }

        /// <summary>
        /// 变量。
        /// </summary>
        public LocalBuilder Value => local ?? throw new EmitException("变量未声明!");

        /// <summary>
        /// 取值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Emit(ILGenerator ilg)
        {
            ilg.Emit(OpCodes.Ldloc, Value);
        }

        /// <summary>
        /// 将当前堆载顶部的数据赋值给变量。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Assign(ILGenerator ilg)
        {
            ilg.Emit(OpCodes.Stloc, Value);
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令</param>
        /// <param name="value">值</param>
        protected override void AssignCore(ILGenerator ilg, Expression value)
        {
            value.Emit(ilg);

            ilg.Emit(OpCodes.Stloc, Value);
        }
    }
}

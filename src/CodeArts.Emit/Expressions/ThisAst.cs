using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 成员本身。
    /// </summary>
    [DebuggerDisplay("this")]
    public class ThisAst : AstExpression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        public ThisAst(Type instanceType) : base(instanceType)
        {
        }

        /// <summary>
        /// 加载。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            ilg.Emit(OpCodes.Ldarg_0);
        }
    }
}

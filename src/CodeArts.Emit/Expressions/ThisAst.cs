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
        /// 单例。
        /// </summary>
        public static ThisAst Instance = new ThisAst();

        /// <summary>
        /// 构造函数。
        /// </summary>
        private ThisAst() : base(typeof(object))
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

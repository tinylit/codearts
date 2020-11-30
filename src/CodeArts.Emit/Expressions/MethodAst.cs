using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 方法。
    /// </summary>
    public class MethodAst : AstExpression
    {
        private readonly AstExpression[] parameters;
        private readonly MethodInfo method;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="method">方法。</param>
        public MethodAst(MethodInfo method) : base(method.ReturnType)
        {
            this.method = method ?? throw new ArgumentNullException(nameof(method));
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="method">方法。</param>
        /// <param name="parameters">参数。</param>
        public MethodAst(MethodInfo method, params AstExpression[] parameters) : this(method)
        {
            this.parameters = parameters;
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (method.IsStatic)
            {
                ilg.Emit(OpCodes.Nop);
            }

            if (parameters != null)
            {
                foreach (var item in parameters)
                {
                    item.Load(ilg);
                }
            }

            if (method.IsStatic || method.DeclaringType.IsValueType)
            {
                ilg.Emit(OpCodes.Call, method);
            }
            else
            {
                ilg.Emit(OpCodes.Callvirt, method);
            }
        }
    }
}

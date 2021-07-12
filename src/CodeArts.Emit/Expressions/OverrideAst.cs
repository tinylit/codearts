using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 重写方法。
    /// </summary>
    public class OverrideAst : AstExpression
    {
        private readonly MethodEmitter methodEmitter;
        private readonly MethodInfo methodInfo;
        private readonly FieldEmitter fieldEmitter;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="fieldEmitter">发行方法。</param>
        /// <param name="methodInfo">被重写方法。</param>
        internal OverrideAst(FieldEmitter fieldEmitter, MethodInfo methodInfo) : base(typeof(MethodInfo))
        {
            this.fieldEmitter = fieldEmitter ?? throw new ArgumentNullException(nameof(fieldEmitter));
            this.methodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="methodEmitter">发行方法。</param>
        /// <param name="methodInfo">被重写方法。</param>
        internal OverrideAst(MethodEmitter methodEmitter, MethodInfo methodInfo) : base(typeof(MethodInfo))
        {
            this.methodEmitter = methodEmitter ?? throw new ArgumentNullException(nameof(methodEmitter));
            this.methodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
        }

        /// <summary>
        /// 函数。
        /// </summary>
        internal MethodInfo Value
        {
            get
            {
                if (methodInfo.IsGenericMethod && methodEmitter.Value.IsGenericMethod)
                {
                    return methodInfo.MakeGenericMethod(methodEmitter.Value.GetGenericArguments());
                }

                return methodInfo;
            }
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (fieldEmitter is null)
            {
                EmitUtils.EmitConstantOfType(ilg, Value, RuntimeType);
            }
            else
            {
                fieldEmitter.Load(ilg);
            }
        }
    }
}

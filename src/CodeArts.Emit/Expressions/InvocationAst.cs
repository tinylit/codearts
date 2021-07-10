using System;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 调用。
    /// </summary>
    public class InvocationAst : AstExpression
    {
        private readonly AstExpression instanceAst;
        private readonly MethodInfo method;
        private readonly AstExpression arguments;

        private static readonly MethodInfo InvokeMethod = typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), new Type[2] { typeof(object), typeof(object[]) });

        /// <summary>
        /// 静态方法调用。
        /// </summary>
        /// <param name="method">方法。</param>
        /// <param name="arguments">调用参数。</param>
        public InvocationAst(MethodInfo method, AstExpression arguments) : this(null, method, arguments)
        {
        }

        /// <summary>
        /// 方法调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="method">方法。</param>
        /// <param name="arguments">调用参数。</param>
        public InvocationAst(AstExpression instanceAst, MethodInfo method, AstExpression arguments) : base(method.ReturnType)
        {
            if (instanceAst is null)
            {
                throw new ArgumentNullException(nameof(instanceAst));
            }

            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (method.IsStatic ^ !(instanceAst is null))
            {
                if (method.IsStatic)
                {
                    throw new ArgumentException($"方法“{method.Name}”是静态的，不能指定实例！");
                }
                else
                {
                    throw new ArgumentException($"方法“{method.Name}”不是静态的，必须指定实例！");
                }
            }

            if (!arguments.ReturnType.IsArray)
            {
                throw new ArgumentException("参数不是数组!", nameof(arguments));
            }

            if (arguments.ReturnType != typeof(object[]))
            {
                throw new ArgumentException("参数不是“System.Object”数组!", nameof(arguments));
            }

            this.instanceAst = instanceAst;
            this.method = method;
            this.arguments = arguments;
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            EmitUtils.EmitConstantOfType(ilg, method, typeof(MethodInfo));

            if (instanceAst is null)
            {
                ilg.Emit(OpCodes.Ldnull);
            }
            else
            {
                instanceAst.Load(ilg);
            }

            arguments.Load(ilg);

            ilg.Emit(OpCodes.Callvirt, InvokeMethod);
        }
    }
}

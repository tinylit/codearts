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
        private readonly AstExpression methodAst;
        private readonly AstExpression arguments;

        private static readonly MethodInfo InvokeMethod = typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), new Type[2] { typeof(object), typeof(object[]) });

        /// <summary>
        /// 静态方法调用。
        /// </summary>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">调用参数。</param>
        public InvocationAst(MethodInfo methodInfo, AstExpression arguments) : this(null, methodInfo, arguments)
        {
        }

        /// <summary>
        /// 方法调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">调用参数。</param>
        public InvocationAst(AstExpression instanceAst, MethodInfo methodInfo, AstExpression arguments) : base(methodInfo is DynamicMethod dynamicMethod ? dynamicMethod.DynamicReturnType : methodInfo.ReturnType)
        {
            if (instanceAst is null)
            {
                throw new ArgumentNullException(nameof(instanceAst));
            }

            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (methodInfo.IsStatic ^ (instanceAst is null))
            {
                if (methodInfo.IsStatic)
                {
                    throw new ArgumentException($"方法“{methodInfo.Name}”是静态的，不能指定实例！");
                }
                else
                {
                    throw new ArgumentException($"方法“{methodInfo.Name}”不是静态的，必须指定实例！");
                }
            }

            if (!arguments.RuntimeType.IsArray)
            {
                throw new ArgumentException("参数不是数组!", nameof(arguments));
            }

            if (arguments.RuntimeType != typeof(object[]))
            {
                throw new ArgumentException("参数不是“System.Object”数组!", nameof(arguments));
            }

            this.instanceAst = instanceAst;
            this.methodAst = new ConstantAst(methodInfo, typeof(MethodInfo));
            this.arguments = arguments;
        }

        /// <summary>
        /// 静态方法调用。
        /// </summary>
        /// <param name="methodAst">方法。</param>
        /// <param name="arguments">调用参数。</param>
        public InvocationAst(AstExpression methodAst, AstExpression arguments) : this(null, methodAst, arguments)
        {
        }

        /// <summary>
        /// 方法调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodAst">方法。</param>
        /// <param name="arguments">调用参数。</param>
        public InvocationAst(AstExpression instanceAst, AstExpression methodAst, AstExpression arguments) : base(typeof(object))
        {
            if (instanceAst is null)
            {
                throw new ArgumentNullException(nameof(instanceAst));
            }

            if (methodAst is null)
            {
                throw new ArgumentNullException(nameof(methodAst));
            }

            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (!arguments.RuntimeType.IsArray)
            {
                throw new ArgumentException("参数不是数组!", nameof(arguments));
            }

            if (arguments.RuntimeType != typeof(object[]))
            {
                throw new ArgumentException("参数不是“System.Object”数组!", nameof(arguments));
            }

            if (methodAst.RuntimeType == typeof(MethodInfo) || typeof(MethodInfo).IsAssignableFrom(methodAst.RuntimeType))
            {
                this.instanceAst = instanceAst;
                this.methodAst = methodAst;
                this.arguments = arguments;
            }
            else
            {
                throw new ArgumentException("参数不是方法!", nameof(methodAst));
            }
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            methodAst.Load(ilg);

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

            if (RuntimeType != typeof(object))
            {
                if (RuntimeType == typeof(void))
                {
                    ilg.Emit(OpCodes.Pop);
                }
                else
                {
                    EmitUtils.EmitConvertToType(ilg, typeof(object), RuntimeType.IsByRef ? RuntimeType.GetElementType() : RuntimeType, true);
                }
            }
        }
    }
}

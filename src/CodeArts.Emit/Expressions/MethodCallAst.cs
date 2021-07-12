using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 调用表达式。
    /// </summary>
    public class MethodCallAst : AstExpression
    {
        private readonly bool isOverrideAst = false;
        private readonly MethodInfo method;
        private readonly AstExpression instanceAst;
        private readonly OverrideAst overrideAst;
        private readonly AstExpression[] arguments;

        private static Type GetReturnType(AstExpression instanceAst, MethodInfo method, AstExpression[] arguments)
        {
            if (method is null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }


            if (method.IsStatic ^ (instanceAst is null))
            {
                if (method.IsStatic)
                {
                    throw new AstException($"方法“{method.Name}”是静态的，不能指定实例！");
                }
                else
                {
                    throw new AstException($"方法“{method.Name}”不是静态的，必须指定实例！");
                }
            }

            var parameterInfos = method.GetParameters();

            if (arguments.Length != parameterInfos.Length)
            {
                throw new AstException("方法参数不匹配!");
            }

            if (parameterInfos.Zip(arguments, (x, y) =>
             {
                 return x.ParameterType == y.RuntimeType || x.ParameterType.IsAssignableFrom(y.RuntimeType);
             }).All(x => x))
            {
                return method.ReturnType;
            }

            throw new AstException("方法参数类型不匹配!");
        }

        /// <summary>
        /// 静态无参函数调用。
        /// </summary>
        /// <param name="method">函数。</param>
        public MethodCallAst(MethodInfo method) : this(null, method, EmptyAsts)
        {
        }

        /// <summary>
        /// 静态函数调用。
        /// </summary>
        /// <param name="method">函数。</param>
        /// <param name="arguments">参数。</param>
        public MethodCallAst(MethodInfo method, AstExpression[] arguments) : this(null, method, arguments)
        {
        }

        /// <summary>
        /// 无参函数调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="method">函数。</param>
        public MethodCallAst(AstExpression instanceAst, MethodInfo method) : this(instanceAst, method, EmptyAsts)
        {
        }

        /// <summary>
        /// 函数调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="method">函数。</param>
        /// <param name="arguments">参数。</param>
        public MethodCallAst(AstExpression instanceAst, MethodInfo method, AstExpression[] arguments) : base(GetReturnType(instanceAst, method, arguments))
        {
            this.instanceAst = instanceAst is null || method.DeclaringType == instanceAst.RuntimeType
                ? instanceAst
                : Convert(instanceAst, method.DeclaringType);

            this.method = method;
            this.arguments = arguments;
        }

        /// <summary>
        /// 静态无参函数调用。
        /// </summary>
        /// <param name="overrideAst">重写方法。</param>
        public MethodCallAst(OverrideAst overrideAst) : this(null, overrideAst, EmptyAsts)
        {
        }

        /// <summary>
        /// 静态函数调用。
        /// </summary>
        /// <param name="overrideAst">重写方法。</param>
        /// <param name="arguments">参数。</param>
        public MethodCallAst(OverrideAst overrideAst, AstExpression[] arguments) : this(null, overrideAst, arguments)
        {
        }

        /// <summary>
        /// 无参函数调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="overrideAst">重写方法。</param>
        public MethodCallAst(AstExpression instanceAst, OverrideAst overrideAst) : this(instanceAst, overrideAst, EmptyAsts)
        {
        }

        /// <summary>
        /// 函数调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="overrideAst">重写方法。</param>
        /// <param name="arguments">参数。</param>
        public MethodCallAst(AstExpression instanceAst, OverrideAst overrideAst, AstExpression[] arguments) : this(instanceAst, overrideAst.Value, arguments)
        {
            isOverrideAst = true;

            this.overrideAst = overrideAst;
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (!method.IsStatic)
            {
                instanceAst.Load(ilg);
            }

            foreach (var item in arguments)
            {
                item.Load(ilg);
            }

            if (method.IsStatic || method.DeclaringType.IsValueType)
            {
                ilg.Emit(OpCodes.Call, isOverrideAst ? overrideAst.Value : method);
            }
            else
            {
                ilg.Emit(OpCodes.Callvirt, isOverrideAst ? overrideAst.Value : method);
            }
        }
    }
}

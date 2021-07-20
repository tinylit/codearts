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
        private readonly MethodInfo methodInfo;
        private readonly AstExpression instanceAst;
        private readonly AstExpression[] arguments;

        private static Type GetReturnType(AstExpression instanceAst, MethodInfo methodInfo, AstExpression[] arguments)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (arguments is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }


            if (methodInfo.IsStatic ^ (instanceAst is null))
            {
                if (methodInfo.IsStatic)
                {
                    throw new AstException($"方法“{methodInfo.Name}”是静态的，不能指定实例！");
                }
                else
                {
                    throw new AstException($"方法“{methodInfo.Name}”不是静态的，必须指定实例！");
                }
            }

            var parameterInfos = methodInfo.GetParameters();

            if (arguments.Length != parameterInfos.Length)
            {
                throw new AstException("方法参数不匹配!");
            }

            if (methodInfo is DynamicMethod dynamicMethod)
            {
                if (parameterInfos.Zip(arguments, (x, y) =>
                 {
                     return x.ParameterType == y.RuntimeType || x.ParameterType.IsAssignableFrom(y.RuntimeType) || EqualSignatureTypes(x.ParameterType, y.RuntimeType);
                 }).All(x => x))
                {
                    return dynamicMethod.ReturnType;
                }
            }
            else if (parameterInfos.Zip(arguments, (x, y) =>
            {
                return x.ParameterType == y.RuntimeType || x.ParameterType.IsAssignableFrom(y.RuntimeType);

            }).All(x => x))
            {
                return methodInfo.ReturnType;
            }

            throw new AstException("方法参数类型不匹配!");
        }

        /// <summary>
        /// 静态无参函数调用。
        /// </summary>
        /// <param name="methodInfo">函数。</param>
        public MethodCallAst(MethodInfo methodInfo) : this(null, methodInfo, EmptyAsts)
        {
        }

        /// <summary>
        /// 静态函数调用。
        /// </summary>
        /// <param name="methodInfo">函数。</param>
        /// <param name="arguments">参数。</param>
        public MethodCallAst(MethodInfo methodInfo, AstExpression[] arguments) : this(null, methodInfo, arguments)
        {
        }

        /// <summary>
        /// 无参函数调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">函数。</param>
        public MethodCallAst(AstExpression instanceAst, MethodInfo methodInfo) : this(instanceAst, methodInfo, EmptyAsts)
        {
        }

        /// <summary>
        /// 函数调用。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">函数。</param>
        /// <param name="arguments">参数。</param>
        public MethodCallAst(AstExpression instanceAst, MethodInfo methodInfo, AstExpression[] arguments) : base(GetReturnType(instanceAst, methodInfo, arguments))
        {
            this.instanceAst = instanceAst is null || methodInfo.DeclaringType == instanceAst.RuntimeType
                ? instanceAst
                : Convert(instanceAst, methodInfo.DeclaringType);

            this.methodInfo = methodInfo;
            this.arguments = arguments;
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (!methodInfo.IsStatic)
            {
                instanceAst.Load(ilg);
            }

            foreach (var item in arguments)
            {
                item.Load(ilg);
            }

            if (methodInfo is DynamicMethod dynamicMethod)
            {
                if (methodInfo.IsStatic || methodInfo.DeclaringType.IsValueType)
                {
                    ilg.Emit(OpCodes.Call, dynamicMethod.RuntimeMethod);
                }
                else
                {
                    ilg.Emit(OpCodes.Callvirt, dynamicMethod.RuntimeMethod);
                }
            }
            else
            {
                if (methodInfo.IsStatic || methodInfo.DeclaringType.IsValueType)
                {
                    ilg.Emit(OpCodes.Call, methodInfo);
                }
                else
                {
                    ilg.Emit(OpCodes.Callvirt, methodInfo);
                }
            }
        }

        private static bool EqualSignatureTypes(Type x, Type y)
        {
            if (x.IsGenericParameter != y.IsGenericParameter)
            {
                return false;
            }
            else if (x.IsGenericType != y.IsGenericType)
            {
                return false;
            }

            if (x.IsGenericParameter)
            {
                if (x.GenericParameterPosition != y.GenericParameterPosition)
                {
                    return false;
                }
            }
            else if (x.IsGenericType)
            {
                var xGenericTypeDef = x.GetGenericTypeDefinition();
                var yGenericTypeDef = y.GetGenericTypeDefinition();

                if (xGenericTypeDef != yGenericTypeDef)
                {
                    return false;
                }

                var xArgs = x.GetGenericArguments();
                var yArgs = y.GetGenericArguments();

                if (xArgs.Length != yArgs.Length)
                {
                    return false;
                }

                for (var i = 0; i < xArgs.Length; ++i)
                {
                    if (!EqualSignatureTypes(xArgs[i], yArgs[i]))
                        return false;
                }
            }
            else
            {
                if (!x.Equals(y))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 创建新实例。
    /// </summary>
    [DebuggerDisplay("new {RuntimeType.Name}(...args)")]
    public class NewInstanceAst : AstExpression
    {
        private readonly ConstructorInfo constructorInfo;
        private readonly AstExpression[] parameters;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="constructorInfo">构造函数。</param>
        public NewInstanceAst(ConstructorInfo constructorInfo) : this(constructorInfo, new AstExpression[0])
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="constructorInfo">构造函数。</param>
        /// <param name="parameters">参数。</param>
        public NewInstanceAst(ConstructorInfo constructorInfo, params AstExpression[] parameters) : base(constructorInfo.DeclaringType)
        {
            ArgumentsCheck(constructorInfo, parameters);

            this.constructorInfo = constructorInfo;
            this.parameters = parameters;
        }

        private static void ArgumentsCheck(ConstructorInfo constructorInfo, AstExpression[] parameters)
        {
            var parameterInfos = constructorInfo.GetParameters();

            if (parameters?.Length != parameterInfos.Length)
            {
                throw new AstException("指定参数和构造函数参数个数不匹配!");
            }

            if (!parameterInfos.Zip(parameters, (x, y) =>
            {
                return x.ParameterType == y.RuntimeType || x.ParameterType.IsAssignableFrom(y.RuntimeType);

            }).All(x => x))
            {
                throw new AstException("指定参数和构造函数参数类型不匹配!");
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        public NewInstanceAst(Type instanceType) : this(instanceType.GetConstructor(Type.EmptyTypes)) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        /// <param name="parameters">参数。</param>
        public NewInstanceAst(Type instanceType, params AstExpression[] parameters) : this(parameters?.Length > 0 ? instanceType.GetConstructor(parameters.Select(x => x.RuntimeType).ToArray()) : instanceType.GetConstructor(Type.EmptyTypes), parameters) { }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (parameters?.Length > 0)
            {
                foreach (var parameter in parameters)
                {
                    parameter.Load(ilg);
                }
            }

            ilg.Emit(OpCodes.Newobj, constructorInfo);
        }
    }
}

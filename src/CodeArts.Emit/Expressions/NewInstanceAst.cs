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
    [DebuggerDisplay("new {ReturnType.Name}({ParemetersNames})")]
    public class NewInstanceAst : AstExpression
    {
        private readonly ConstructorInfo constructorInfo;
        private readonly AstExpression[] parameters;
        private string ParemetersNames => string.Join(",", parameters.Select(x => x.ReturnType.Name));

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="constructorInfo">构造函数。</param>
        /// <param name="parameters">参数。</param>
        public NewInstanceAst(ConstructorInfo constructorInfo, params AstExpression[] parameters) : base(constructorInfo.DeclaringType)
        {
            this.constructorInfo = constructorInfo ?? throw new ArgumentNullException(nameof(constructorInfo));
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

            if (parameters.Length != constructorInfo.GetParameters().Length)
            {
                throw new AstException("指定参数和构造函数参数个数不匹配!");
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        /// <param name="parameters">参数。</param>
        public NewInstanceAst(Type instanceType, params AstExpression[] parameters) : this(instanceType.GetConstructor(parameters?.Select(x => x.ReturnType).ToArray() ?? Type.EmptyTypes), parameters) { }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (parameters != null)
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

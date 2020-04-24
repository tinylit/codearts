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
    [DebuggerDisplay("new {ReturnType.Name}()")]
    public class NewInstanceAst : AstExpression
    {
        private readonly ConstructorInfo constructorInfo;
        private readonly AstExpression[] paramters;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="constructorInfo">构造函数</param>
        /// <param name="paramters">参数</param>
        public NewInstanceAst(ConstructorInfo constructorInfo, params AstExpression[] paramters) : base(constructorInfo.DeclaringType)
        {
            this.constructorInfo = constructorInfo ?? throw new ArgumentNullException(nameof(constructorInfo));
            this.paramters = paramters;
            var parameters = constructorInfo.GetParameters();

            if (paramters.Length != parameters.Length)
            {
                throw new AstException("指定参数和构造函数参数个数不匹配!");
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        /// <param name="paramters">参数</param>
        public NewInstanceAst(Type instanceType, params AstExpression[] paramters) : this(instanceType.GetConstructor(paramters?.Select(x => x.ReturnType).ToArray() ?? Type.EmptyTypes), paramters) { }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Load(ILGenerator ilg)
        {
            if (paramters != null)
            {
                foreach (var paramter in paramters)
                {
                    paramter.Load(ilg);
                }
            }

            ilg.Emit(OpCodes.Newobj, constructorInfo);
        }
    }
}

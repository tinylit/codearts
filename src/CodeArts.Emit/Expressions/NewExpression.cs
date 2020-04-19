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
    public class NewExpression : Expression
    {
        private readonly ConstructorInfo constructorInfo;
        private readonly Expression[] expressions;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="constructorInfo">构造函数</param>
        /// <param name="expressions">参数</param>
        public NewExpression(ConstructorInfo constructorInfo, params Expression[] expressions) : base(constructorInfo.DeclaringType)
        {
            this.constructorInfo = constructorInfo ?? throw new ArgumentNullException(nameof(constructorInfo));
            this.expressions = expressions ?? new Expression[0];
            var parameters = constructorInfo.GetParameters();

            if (expressions.Length != parameters.Length)
            {
                throw new EmitException("指定参数和构造函数参数个数不匹配!");
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        /// <param name="expressions">参数</param>
        public NewExpression(Type instanceType, params Expression[] expressions) : this(instanceType.GetConstructor(expressions?.Select(x => x.ReturnType).ToArray() ?? Type.EmptyTypes), expressions) { }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Emit(ILGenerator ilg)
        {
            foreach (var expression in expressions)
            {
                expression.Emit(ilg);
            }

            ilg.Emit(OpCodes.Newobj, constructorInfo);
        }
    }
}

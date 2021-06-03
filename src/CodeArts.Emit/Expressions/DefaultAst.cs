using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 默认值。
    /// </summary>
    [DebuggerDisplay("default({Type.Name})")]
    public class DefaultAst : AstExpression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="defaultType">类型。</param>
        public DefaultAst(Type defaultType) : base(defaultType)
        {
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (ReturnType.IsValueType || ReturnType.IsGenericParameter)
            {
                EmitUtils.EmitDefaultValueOfType(ilg, ReturnType);
            }
            else if (ReturnType.IsByRef)
            {
                var elementType = ReturnType.GetElementType();

                if (elementType.IsGenericParameter || elementType.IsValueType)
                {
                    ilg.Emit(OpCodes.Initobj, elementType);
                }
                else
                {
                    EmitUtils.EmitDefaultValueOfType(ilg, elementType);
                    EmitUtils.EmitAssignToType(ilg, elementType);
                }
            }
            else
            {
                EmitUtils.EmitDefaultValueOfType(ilg, ReturnType);
            }
        }
    }
}

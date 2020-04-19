using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 默认值。
    /// </summary>
    [DebuggerDisplay("default({Type.Name})")]
    public class DefaultExpression : Expression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="type">类型。</param>
        public DefaultExpression(Type type) : base(type)
        {
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Emit(ILGenerator ilg)
        {
            if (IsPrimitiveOrClass(ReturnType))
            {
                EmitCodes.EmitDefaultValueOfType(ilg, ReturnType);
            }
            else if (ReturnType.IsValueType || ReturnType.IsGenericParameter)
            {
                var local = ilg.DeclareLocal(ReturnType);
                ilg.Emit(OpCodes.Ldloca_S, local);
                ilg.Emit(OpCodes.Initobj, ReturnType);
                ilg.Emit(OpCodes.Ldloc, local);
            }
            else if (ReturnType.IsByRef)
            {
                EmitByRef(ilg, ReturnType);
            }
            else
            {
                throw new EmitException($"不能发行“{ReturnType}”类型的默认值!");
            }
        }

        private static void EmitByRef(ILGenerator ilg, Type type)
        {
            var elementType = type.GetElementType();

            if (IsPrimitiveOrClass(elementType))
            {
                EmitCodes.EmitDefaultValueOfType(ilg, elementType);
                EmitCodes.EmitAssignIndirectOpCodeForType(ilg, elementType);
            }
            else if (elementType.IsGenericParameter || elementType.IsValueType)
            {
                ilg.Emit(OpCodes.Initobj, elementType);
            }
            else
            {
                throw new EmitException($"不能发行引用“{elementType}”类型的默认值!");
            }
        }

        private static bool IsPrimitiveOrClass(Type type)
        {
            return type.IsPrimitive && type != typeof(IntPtr) || (type.IsClass || type.IsInterface) && type.IsGenericParameter == false && type.IsByRef == false;
        }
    }
}

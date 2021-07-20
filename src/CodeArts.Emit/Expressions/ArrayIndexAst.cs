using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 数组索引。
    /// </summary>
    [DebuggerDisplay("{array}[{index}]")]
    public class ArrayIndexAst : AstExpression
    {
        private readonly AstExpression array;
        private readonly int index = -1;
        private readonly AstExpression indexExp;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="array">数组。</param>
        /// <param name="index">索引。</param>
        public ArrayIndexAst(AstExpression array, int index) : base(array.RuntimeType.GetElementType())
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            this.array = array ?? throw new ArgumentNullException(nameof(array));

            if (array.RuntimeType.IsArray && typeof(Array).IsAssignableFrom(array.RuntimeType) && array.RuntimeType.GetArrayRank() == 1)
            {
                this.index = index;
            }
            else
            {
                throw new ArgumentException("不是数组，或不是一维数组!", nameof(array));
            }
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="array">数组。</param>
        /// <param name="indexExp">索引。</param>
        public ArrayIndexAst(AstExpression array, AstExpression indexExp) : base(array.RuntimeType.GetElementType())
        {
            this.array = array ?? throw new ArgumentNullException(nameof(array));
            if (array.RuntimeType.IsArray && typeof(Array).IsAssignableFrom(array.RuntimeType) && array.RuntimeType.GetArrayRank() == 1)
            {
                this.indexExp = indexExp ?? throw new ArgumentNullException(nameof(indexExp));
            }
            else
            {
                throw new ArgumentException("不是数组，或不是一维数组!", nameof(array));
            }
        }

        /// <summary>
        /// 是否可写。
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// 取值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            array.Load(ilg);

            if (index == -1)
            {
                indexExp.Load(ilg);
            }
            else
            {
                EmitUtils.EmitInt(ilg, index);
            }

            EmitEnsureArrayIndexLoadedSafely(ilg, RuntimeType);
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void AssignCore(ILGenerator ilg, AstExpression value)
        {
            if (index == -1)
            {
                if (indexExp is VariableAst variable)
                {
                    array.Load(ilg);

                    ilg.Emit(OpCodes.Ldc_I4, variable.Value);
                }
                else
                {
                    var local = ilg.DeclareLocal(typeof(int));

                    indexExp.Load(ilg);

                    ilg.Emit(OpCodes.Stloc, local);

                    array.Load(ilg);

                    ilg.Emit(OpCodes.Ldc_I4, local);
                }
            }
            else
            {
                array.Load(ilg);

                EmitUtils.EmitInt(ilg, index);
            }

            value.Load(ilg);

            EmitEnsureArrayIndexAssignedSafely(ilg, RuntimeType);
        }

        private static void EmitEnsureArrayIndexLoadedSafely(ILGenerator ilg, Type elementType)
        {
            if (!elementType.IsValueType)
            {
                ilg.Emit(OpCodes.Ldelem_Ref);
            }
            else if (elementType.IsEnum)
            {
                ilg.Emit(OpCodes.Ldelem, elementType);
            }
            else
            {
                switch (Type.GetTypeCode(elementType))
                {
                    case TypeCode.Boolean:
                    case TypeCode.SByte:
                        ilg.Emit(OpCodes.Ldelem_I1);
                        break;
                    case TypeCode.Byte:
                        ilg.Emit(OpCodes.Ldelem_U1);
                        break;
                    case TypeCode.Int16:
                        ilg.Emit(OpCodes.Ldelem_I2);
                        break;
                    case TypeCode.Char:
                    case TypeCode.UInt16:
                        ilg.Emit(OpCodes.Ldelem_U2);
                        break;
                    case TypeCode.Int32:
                        ilg.Emit(OpCodes.Ldelem_I4);
                        break;
                    case TypeCode.UInt32:
                        ilg.Emit(OpCodes.Ldelem_U4);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        ilg.Emit(OpCodes.Ldelem_I8);
                        break;
                    case TypeCode.Single:
                        ilg.Emit(OpCodes.Ldelem_R4);
                        break;
                    case TypeCode.Double:
                        ilg.Emit(OpCodes.Ldelem_R8);
                        break;
                    default:
                        ilg.Emit(OpCodes.Ldelem, elementType);
                        break;
                }
            }
        }

        private static void EmitEnsureArrayIndexAssignedSafely(ILGenerator ilg, Type elementType)
        {
            if (elementType.IsValueType && elementType.IsEnum)
            {
                ilg.Emit(OpCodes.Stelem, elementType);
            }
            else
            {
                switch (Type.GetTypeCode(elementType))
                {
                    case TypeCode.Boolean:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        ilg.Emit(OpCodes.Stelem_I1);
                        break;
                    case TypeCode.Char:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        ilg.Emit(OpCodes.Stelem_I2);
                        break;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        ilg.Emit(OpCodes.Stelem_I4);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        ilg.Emit(OpCodes.Stelem_I8);
                        break;
                    case TypeCode.Single:
                        ilg.Emit(OpCodes.Stelem_R4);
                        break;
                    case TypeCode.Double:
                        ilg.Emit(OpCodes.Stelem_R8);
                        break;
                    default:
                        if (elementType.IsValueType)
                        {
                            ilg.Emit(OpCodes.Stelem, elementType);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Stelem_Ref);
                        }
                        break;
                }
            }
        }
    }
}

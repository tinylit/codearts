using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 数组索引。
    /// </summary>
    [DebuggerDisplay("{array}[{index}]")]
    public class ArrayIndexAst : AssignAstExpression
    {
        private readonly AstExpression array;
        private readonly int index = -1;
        private readonly AstExpression indexExp;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="array">数组。</param>
        /// <param name="index">索引。</param>
        public ArrayIndexAst(AstExpression array, int index) : base(array.ReturnType.GetElementType())
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            this.array = array ?? throw new ArgumentNullException(nameof(array));

            if (array.ReturnType.IsArray && typeof(Array).IsAssignableFrom(array.ReturnType) && array.ReturnType.GetArrayRank() == 1)
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
        public ArrayIndexAst(AstExpression array, AstExpression indexExp) : base(array.ReturnType.GetElementType())
        {
            this.array = array ?? throw new ArgumentNullException(nameof(array));
            if (array.ReturnType.IsArray && typeof(Array).IsAssignableFrom(array.ReturnType) && array.ReturnType.GetArrayRank() == 1)
            {
                this.indexExp = indexExp ?? throw new ArgumentNullException(nameof(indexExp));
            }
            else
            {
                throw new ArgumentException("不是数组，或不是一维数组!", nameof(array));
            }
        }

        private static void EmitInt(ILGenerator ilg, int value)
        {
            OpCode c;
            switch (value)
            {
                case -1:
                    c = OpCodes.Ldc_I4_M1;
                    break;
                case 0:
                    c = OpCodes.Ldc_I4_0;
                    break;
                case 1:
                    c = OpCodes.Ldc_I4_1;
                    break;
                case 2:
                    c = OpCodes.Ldc_I4_2;
                    break;
                case 3:
                    c = OpCodes.Ldc_I4_3;
                    break;
                case 4:
                    c = OpCodes.Ldc_I4_4;
                    break;
                case 5:
                    c = OpCodes.Ldc_I4_5;
                    break;
                case 6:
                    c = OpCodes.Ldc_I4_6;
                    break;
                case 7:
                    c = OpCodes.Ldc_I4_7;
                    break;
                case 8:
                    c = OpCodes.Ldc_I4_8;
                    break;
                default:
                    if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                    {
                        ilg.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Ldc_I4, value);
                    }
                    return;
            }
            ilg.Emit(c);
        }

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
                EmitInt(ilg, index);
            }

            EmitEnsureArrayIndexLoadedSafely(ilg, ReturnType);
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Assign(ILGenerator ilg) => throw new NotSupportedException();

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

                EmitInt(ilg, index);
            }

            value.Load(ilg);

            EmitEnsureArrayIndexAssignedSafely(ilg, ReturnType);
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
            if (elementType.IsEnum)
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

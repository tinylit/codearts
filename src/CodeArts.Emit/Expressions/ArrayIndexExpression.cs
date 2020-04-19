using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 数组索引。
    /// </summary>
    [DebuggerDisplay("{array}[{index}]")]
    public class ArrayIndexExpression : AssignedExpression
    {
        private readonly Expression array;
        private readonly int index = -1;
        private readonly Expression indexExp;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="index">索引</param>
        public ArrayIndexExpression(Expression array, int index) : base(array.ReturnType.GetElementType())
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            this.array = array ?? throw new ArgumentNullException(nameof(array));

            if (array.ReturnType.IsArray)
            {
                this.index = index;
            }
            else
            {
                throw new ArgumentException("不是数组!", nameof(array));
            }
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="indexExp">索引</param>
        public ArrayIndexExpression(Expression array, Expression indexExp) : base(array.ReturnType.GetElementType())
        {
            this.array = array ?? throw new ArgumentNullException(nameof(array));
            if (array.ReturnType.IsArray)
            {
                this.indexExp = indexExp ?? throw new ArgumentNullException(nameof(indexExp));
            }
            else
            {
                throw new ArgumentException("不是数组!", nameof(array));
            }
        }

        /// <summary>
        /// 取值。
        /// </summary>
        /// <param name="iLGen">指令。</param>
        public override void Emit(ILGenerator iLGen)
        {
            array.Emit(iLGen);

            if (index == -1)
            {
                indexExp.Emit(iLGen);
            }
            else
            {
                iLGen.Emit(OpCodes.Ldc_I4, index);
            }

            if (!ReturnType.IsValueType)
            {
                iLGen.Emit(OpCodes.Ldelem_Ref);
            }
            else if (ReturnType.IsEnum)
            {
                iLGen.Emit(OpCodes.Ldelem, ReturnType);
            }
            else
            {
                switch (Type.GetTypeCode(ReturnType))
                {
                    case TypeCode.Boolean:
                    case TypeCode.SByte:
                        iLGen.Emit(OpCodes.Ldelem_I1);
                        break;
                    case TypeCode.Byte:
                        iLGen.Emit(OpCodes.Ldelem_U1);
                        break;
                    case TypeCode.Int16:
                        iLGen.Emit(OpCodes.Ldelem_I2);
                        break;
                    case TypeCode.Char:
                    case TypeCode.UInt16:
                        iLGen.Emit(OpCodes.Ldelem_U2);
                        break;
                    case TypeCode.Int32:
                        iLGen.Emit(OpCodes.Ldelem_I4);
                        break;
                    case TypeCode.UInt32:
                        iLGen.Emit(OpCodes.Ldelem_U4);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        iLGen.Emit(OpCodes.Ldelem_I8);
                        break;
                    case TypeCode.Single:
                        iLGen.Emit(OpCodes.Ldelem_R4);
                        break;
                    case TypeCode.Double:
                        iLGen.Emit(OpCodes.Ldelem_R8);
                        break;
                    default:
                        iLGen.Emit(OpCodes.Ldelem, ReturnType);
                        break;
                }
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="iLGen">指令。</param>
        /// <param name="value">值</param>
        protected override void AssignCore(ILGenerator iLGen, Expression value)
        {

            if (index == -1)
            {
                if (indexExp is VariableExpression variable)
                {
                    array.Emit(iLGen);

                    iLGen.Emit(OpCodes.Ldc_I4, variable.Value);
                }
                else
                {
                    var local = iLGen.DeclareLocal(typeof(int));

                    indexExp.Emit(iLGen);

                    iLGen.Emit(OpCodes.Stloc, local);

                    array.Emit(iLGen);

                    iLGen.Emit(OpCodes.Ldc_I4, local);
                }
            }
            else
            {
                array.Emit(iLGen);

                iLGen.Emit(OpCodes.Ldc_I4, index);
            }

            value.Emit(iLGen);

            if (ReturnType.IsEnum)
            {
                iLGen.Emit(OpCodes.Stelem, ReturnType);
            }
            else
            {
                switch (Type.GetTypeCode(ReturnType))
                {
                    case TypeCode.Boolean:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        iLGen.Emit(OpCodes.Stelem_I1);
                        break;
                    case TypeCode.Char:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        iLGen.Emit(OpCodes.Stelem_I2);
                        break;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        iLGen.Emit(OpCodes.Stelem_I4);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        iLGen.Emit(OpCodes.Stelem_I8);
                        break;
                    case TypeCode.Single:
                        iLGen.Emit(OpCodes.Stelem_R4);
                        break;
                    case TypeCode.Double:
                        iLGen.Emit(OpCodes.Stelem_R8);
                        break;
                    default:
                        if (ReturnType.IsValueType)
                        {
                            iLGen.Emit(OpCodes.Stelem, ReturnType);
                        }
                        else
                        {
                            iLGen.Emit(OpCodes.Stelem_Ref);
                        }
                        break;
                }
            }
        }
    }
}

using System;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 一元运算符。
    /// </summary>
    public class UnaryAst : AstExpression
    {
        private readonly UnaryExpressionType expressionType;
        private readonly AstExpression body;

        private UnaryAst(UnaryAst unaryAst) : base(unaryAst.RuntimeType)
        {
            expressionType = unaryAst.expressionType - 1;
            body = unaryAst.body;
        }

        /// <summary>
        /// 一元运算。
        /// </summary>
        /// <param name="expressionType">一元运算类型。</param>
        /// <param name="body">表达式。</param>
        public UnaryAst(AstExpression body, UnaryExpressionType expressionType) : base(AnalysisType(expressionType, body))
        {
            this.expressionType = expressionType;
            this.body = body;
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (expressionType < UnaryExpressionType.UnaryPlus && (expressionType & UnaryExpressionType.Increment) == 0)
            {
                Assign(body, new UnaryAst(this))
                    .Load(ilg);
            }
            else
            {
                body.Load(ilg);

                switch (expressionType)
                {
                    case UnaryExpressionType.UnaryPlus:
                        ilg.Emit(OpCodes.Nop);
                        break;
                    case UnaryExpressionType.Negate:
                        ilg.Emit(OpCodes.Neg);
                        break;
                    case UnaryExpressionType.Not:
                        if (RuntimeType == typeof(bool))
                        {
                            ilg.Emit(OpCodes.Ldc_I4_0);
                            ilg.Emit(OpCodes.Ceq);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Not);
                        }
                        break;
                    case UnaryExpressionType.Increment:
                        TryEmitConstantOne(ilg, body.RuntimeType);
                        ilg.Emit(OpCodes.Add);
                        break;
                    case UnaryExpressionType.Decrement:
                        TryEmitConstantOne(ilg, body.RuntimeType);
                        ilg.Emit(OpCodes.Sub);
                        break;
                    case UnaryExpressionType.IsFalse:
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        ilg.Emit(OpCodes.Ceq);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }
        private static Type AnalysisType(UnaryExpressionType expressionType, AstExpression body)
        {
            switch (expressionType)
            {

                case UnaryExpressionType.UnaryPlus:
                case UnaryExpressionType.Increment:
                case UnaryExpressionType.Decrement:
                    if (IsArithmetic(body.RuntimeType))
                    {
                        return body.RuntimeType;
                    }
                    break;
                case UnaryExpressionType.Negate:
                    if (IsArithmetic(body.RuntimeType) && !IsUnsignedInt(body.RuntimeType))
                    {
                        return body.RuntimeType;
                    }
                    break;
                case UnaryExpressionType.Not:
                    if (IsIntegerOrBool(body.RuntimeType))
                    {
                        return body.RuntimeType;
                    }
                    break;
                case UnaryExpressionType.IsFalse:
                    if (body.RuntimeType == typeof(bool))
                    {
                        return body.RuntimeType;
                    }
                    break;
                case UnaryExpressionType.IncrementAssign:
                case UnaryExpressionType.DecrementAssign:
                    if (!body.CanWrite)
                    {
                        throw new AstException("表达式不可写!");
                    }
                    goto case UnaryExpressionType.Increment;
            }

            throw new InvalidOperationException($"“{body.RuntimeType}”不支持“{expressionType}”一元操作!");
        }

        private static bool IsUnsignedInt(Type type)
        {
            if (type.IsNullable())
            {
                return false;
            }

            switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsIntegerOrBool(Type type)
        {
            if (type.IsNullable())
            {
                return false;
            }

            switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
            {
                case TypeCode.Int64:
                case TypeCode.Int32:
                case TypeCode.Int16:
                case TypeCode.UInt64:
                case TypeCode.UInt32:
                case TypeCode.UInt16:
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsArithmetic(Type type)
        {
            if (type.IsNullable())
            {
                return false;
            }

            switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private static void TryEmitConstantOne(ILGenerator ilg, Type type)
        {
            switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
            {
                case TypeCode.Byte:
                    EmitUtils.EmitByte(ilg, 1);
                    break;
                case TypeCode.Int16:
                    EmitUtils.EmitInt16(ilg, 1);
                    break;
                case TypeCode.UInt16:
                    EmitUtils.EmitUInt16(ilg, 1);
                    break;
                case TypeCode.Int32:
                    EmitUtils.EmitInt(ilg, 1);
                    break;
                case TypeCode.UInt32:
                    EmitUtils.EmitUInt(ilg, 1);
                    break;
                case TypeCode.Int64:
                    EmitUtils.EmitLong(ilg, 1L);
                    break;
                case TypeCode.UInt64:
                    EmitUtils.EmitULong(ilg, 1uL);
                    break;
                case TypeCode.Single:
                    EmitUtils.EmitSingle(ilg, 1F);
                    break;
                case TypeCode.Double:
                    EmitUtils.EmitDouble(ilg, 1D);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

    }
}

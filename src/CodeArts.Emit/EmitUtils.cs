using CodeArts.Emit.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 工具。
    /// </summary>
    public static class EmitUtils
    {
        private static readonly ConstructorInfo GuidConstructorInfo = typeof(Guid).GetConstructor(new Type[1] { typeof(string) });

        private static readonly MethodInfo GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");

        private static readonly MethodInfo GetIsGenericTypeMethodFromHandle = typeof(MethodBase).GetMethod("GetMethodFromHandle", new[] { typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle) });

        private static readonly List<object> Constants = new List<object>();
        private static readonly Dictionary<object, int> ConstantCache = new Dictionary<object, int>();
        private static readonly MethodInfo GetConstantMethod = typeof(EmitUtils).GetMethod(nameof(GetConstant), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        private static object GetConstant(int index) => Constants[index];

        internal static bool IsNullable(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        #region Convert

        private static bool IsConvertible(Type type)
        {
            if (type.IsNullable())
            {
                type = Nullable.GetUnderlyingType(type);
            }

            if (type.IsEnum)
            {
                return true;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Char:
                    return true;
                default:
                    return false;
            }
        }

        private static bool AreEquivalent(Type t1, Type t2)
        {
#if CLR2 || SILVERLIGHT
            return t1 == t2;
#else
            return t1 == t2 || t1.IsEquivalentTo(t2);
#endif
        }

        private static bool IsDelegate(Type t)
        {
            return t.IsSubclassOf(typeof(MulticastDelegate));
        }

        private static bool IsCovariant(Type t)
        {
            return GenericParameterAttributes.Covariant == (t.GenericParameterAttributes & GenericParameterAttributes.Covariant);
        }

        private static bool IsContravariant(Type t)
        {
            return GenericParameterAttributes.Contravariant == (t.GenericParameterAttributes & GenericParameterAttributes.Contravariant);
        }

        private static bool IsInvariant(Type t)
        {
            return 0 == (t.GenericParameterAttributes & GenericParameterAttributes.VarianceMask);
        }

        private static bool HasReferenceConversion(Type source, Type dest)
        {
            // void -> void conversion is handled elsewhere
            // (it's an identity conversion)
            // All other void conversions are disallowed.
            if (source == typeof(void) || dest == typeof(void))
            {
                return false;
            }

            Type nnSourceType = source.IsNullable() ? Nullable.GetUnderlyingType(source) : source;
            Type nnDestType = dest.IsNullable() ? Nullable.GetUnderlyingType(dest) : dest;

            // Down conversion
            if (nnSourceType.IsAssignableFrom(nnDestType))
            {
                return true;
            }
            // Up conversion
            if (nnDestType.IsAssignableFrom(nnSourceType))
            {
                return true;
            }
            // Interface conversion
            if (source.IsInterface || dest.IsInterface)
            {
                return true;
            }
            // Variant delegate conversion
            if (IsLegalExplicitVariantDelegateConversion(source, dest))
                return true;

            // Object conversion
            if (source == typeof(object) || dest == typeof(object))
            {
                return true;
            }
            return false;
        }

        private static bool IsLegalExplicitVariantDelegateConversion(Type source, Type dest)
        {
            // There *might* be a legal conversion from a generic delegate type S to generic delegate type  T, 
            // provided all of the follow are true:
            //   o Both types are constructed generic types of the same generic delegate type, D<X1,... Xk>.
            //     That is, S = D<S1...>, T = D<T1...>.
            //   o If type parameter Xi is declared to be invariant then Si must be identical to Ti.
            //   o If type parameter Xi is declared to be covariant ("out") then Si must be convertible
            //     to Ti via an identify conversion,  implicit reference conversion, or explicit reference conversion.
            //   o If type parameter Xi is declared to be contravariant ("in") then either Si must be identical to Ti, 
            //     or Si and Ti must both be reference types.

            if (!IsDelegate(source) || !IsDelegate(dest) || !source.IsGenericType || !dest.IsGenericType)
                return false;

            Type genericDelegate = source.GetGenericTypeDefinition();

            if (dest.GetGenericTypeDefinition() != genericDelegate)
                return false;

            Type[] genericParameters = genericDelegate.GetGenericArguments();
            Type[] sourceArguments = source.GetGenericArguments();
            Type[] destArguments = dest.GetGenericArguments();

            Debug.Assert(genericParameters != null);
            Debug.Assert(sourceArguments != null);
            Debug.Assert(destArguments != null);
            Debug.Assert(genericParameters.Length == sourceArguments.Length);
            Debug.Assert(genericParameters.Length == destArguments.Length);

            for (int iParam = 0; iParam < genericParameters.Length; ++iParam)
            {
                Type sourceArgument = sourceArguments[iParam];
                Type destArgument = destArguments[iParam];

                Debug.Assert(sourceArgument != null && destArgument != null);

                // If the arguments are identical then this one is automatically good, so skip it.
                if (AreEquivalent(sourceArgument, destArgument))
                {
                    continue;
                }

                Type genericParameter = genericParameters[iParam];

                Debug.Assert(genericParameter != null);

                if (IsInvariant(genericParameter))
                {
                    return false;
                }

                if (IsCovariant(genericParameter))
                {
                    if (!HasReferenceConversion(sourceArgument, destArgument))
                    {
                        return false;
                    }
                }
                else if (IsContravariant(genericParameter))
                {
                    if (sourceArgument.IsValueType || destArgument.IsValueType)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static void EmitCastToType(ILGenerator ilg, Type typeFrom, Type typeTo)
        {
            if (!typeFrom.IsValueType && typeTo.IsValueType)
            {
                ilg.Emit(OpCodes.Unbox_Any, typeTo);
            }
            else if (typeFrom.IsValueType && !typeTo.IsValueType)
            {
                ilg.Emit(OpCodes.Box, typeFrom);
                if (typeTo != typeof(object))
                {
                    ilg.Emit(OpCodes.Castclass, typeTo);
                }
            }
            else if (!typeFrom.IsValueType && !typeTo.IsValueType)
            {
                ilg.Emit(OpCodes.Castclass, typeTo);
            }
            else
            {
                throw new InvalidCastException();
            }
        }

        private static void EmitHasValue(ILGenerator ilg, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("get_HasValue", BindingFlags.Instance | BindingFlags.Public);
            ilg.Emit(OpCodes.Call, mi);
        }

        private static void EmitGetValue(ILGenerator ilg, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);
            ilg.Emit(OpCodes.Call, mi);
        }

        private static void EmitGetValueOrDefault(ILGenerator ilg, Type nullableType)
        {
            MethodInfo mi = nullableType.GetMethod("GetValueOrDefault", Type.EmptyTypes);
            ilg.Emit(OpCodes.Call, mi);
        }

        private static bool IsUnsigned(Type type)
        {
            if (type.IsNullable())
            {
                type = Nullable.GetUnderlyingType(type);
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.Char:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsFloatingPoint(Type type)
        {
            if (type.IsNullable())
            {
                type = Nullable.GetUnderlyingType(type);
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }

        private static void EmitNumericConversion(ILGenerator ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            bool isFromUnsigned = IsUnsigned(typeFrom);
            bool isFromFloatingPoint = IsFloatingPoint(typeFrom);

            if (typeTo == typeof(float))
            {
                if (isFromUnsigned)
                    ilg.Emit(OpCodes.Conv_R_Un);
                ilg.Emit(OpCodes.Conv_R4);
            }
            else if (typeTo == typeof(double))
            {
                if (isFromUnsigned)
                    ilg.Emit(OpCodes.Conv_R_Un);
                ilg.Emit(OpCodes.Conv_R8);
            }
            else
            {
                TypeCode tc = Type.GetTypeCode(typeTo);
                if (isChecked)
                {
                    // Overflow checking needs to know if the source value on the IL stack is unsigned or not.
                    if (isFromUnsigned)
                    {
                        switch (tc)
                        {
                            case TypeCode.SByte:
                                ilg.Emit(OpCodes.Conv_Ovf_I1_Un);
                                break;
                            case TypeCode.Int16:
                                ilg.Emit(OpCodes.Conv_Ovf_I2_Un);
                                break;
                            case TypeCode.Int32:
                                ilg.Emit(OpCodes.Conv_Ovf_I4_Un);
                                break;
                            case TypeCode.Int64:
                                ilg.Emit(OpCodes.Conv_Ovf_I8_Un);
                                break;
                            case TypeCode.Byte:
                                ilg.Emit(OpCodes.Conv_Ovf_U1_Un);
                                break;
                            case TypeCode.UInt16:
                            case TypeCode.Char:
                                ilg.Emit(OpCodes.Conv_Ovf_U2_Un);
                                break;
                            case TypeCode.UInt32:
                                ilg.Emit(OpCodes.Conv_Ovf_U4_Un);
                                break;
                            case TypeCode.UInt64:
                                ilg.Emit(OpCodes.Conv_Ovf_U8_Un);
                                break;
                            default:
                                throw new InvalidCastException();
                        }
                    }
                    else
                    {
                        switch (tc)
                        {
                            case TypeCode.SByte:
                                ilg.Emit(OpCodes.Conv_Ovf_I1);
                                break;
                            case TypeCode.Int16:
                                ilg.Emit(OpCodes.Conv_Ovf_I2);
                                break;
                            case TypeCode.Int32:
                                ilg.Emit(OpCodes.Conv_Ovf_I4);
                                break;
                            case TypeCode.Int64:
                                ilg.Emit(OpCodes.Conv_Ovf_I8);
                                break;
                            case TypeCode.Byte:
                                ilg.Emit(OpCodes.Conv_Ovf_U1);
                                break;
                            case TypeCode.UInt16:
                            case TypeCode.Char:
                                ilg.Emit(OpCodes.Conv_Ovf_U2);
                                break;
                            case TypeCode.UInt32:
                                ilg.Emit(OpCodes.Conv_Ovf_U4);
                                break;
                            case TypeCode.UInt64:
                                ilg.Emit(OpCodes.Conv_Ovf_U8);
                                break;
                            default:
                                throw new InvalidCastException();
                        }
                    }
                }
                else
                {
                    switch (tc)
                    {
                        case TypeCode.SByte:
                            ilg.Emit(OpCodes.Conv_I1);
                            break;
                        case TypeCode.Byte:
                            ilg.Emit(OpCodes.Conv_U1);
                            break;
                        case TypeCode.Int16:
                            ilg.Emit(OpCodes.Conv_I2);
                            break;
                        case TypeCode.UInt16:
                        case TypeCode.Char:
                            ilg.Emit(OpCodes.Conv_U2);
                            break;
                        case TypeCode.Int32:
                            ilg.Emit(OpCodes.Conv_I4);
                            break;
                        case TypeCode.UInt32:
                            ilg.Emit(OpCodes.Conv_U4);
                            break;
                        case TypeCode.Int64:
                            if (isFromUnsigned)
                            {
                                ilg.Emit(OpCodes.Conv_U8);
                            }
                            else
                            {
                                ilg.Emit(OpCodes.Conv_I8);
                            }
                            break;
                        case TypeCode.UInt64:
                            if (isFromUnsigned || isFromFloatingPoint)
                            {
                                ilg.Emit(OpCodes.Conv_U8);
                            }
                            else
                            {
                                ilg.Emit(OpCodes.Conv_I8);
                            }
                            break;
                        default:
                            throw new InvalidCastException();
                    }
                }
            }
        }

        private static void EmitNonNullableToNullableConversion(ILGenerator ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            LocalBuilder locTo = ilg.DeclareLocal(typeTo);
            Type nnTypeTo = Nullable.GetUnderlyingType(typeTo);
            EmitConvertToType(ilg, typeFrom, nnTypeTo, isChecked);

            ConstructorInfo ci = typeTo.GetConstructor(new Type[] { nnTypeTo });
            ilg.Emit(OpCodes.Newobj, ci);
            ilg.Emit(OpCodes.Stloc, locTo);
            ilg.Emit(OpCodes.Ldloc, locTo);
        }

        private static void EmitNullableToNullableConversion(ILGenerator ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            LocalBuilder locFrom = ilg.DeclareLocal(typeFrom);
            ilg.Emit(OpCodes.Stloc, locFrom);
            LocalBuilder locTo = ilg.DeclareLocal(typeTo);
            // test for null
            ilg.Emit(OpCodes.Ldloca, locFrom);
            EmitHasValue(ilg, typeFrom);
            Label labIfNull = ilg.DefineLabel();
            ilg.Emit(OpCodes.Brfalse_S, labIfNull);
            ilg.Emit(OpCodes.Ldloca, locFrom);
            EmitGetValueOrDefault(ilg, typeFrom);
            Type nnTypeFrom = Nullable.GetUnderlyingType(typeFrom);
            Type nnTypeTo = Nullable.GetUnderlyingType(typeTo);
            EmitConvertToType(ilg, nnTypeFrom, nnTypeTo, isChecked);
            // construct result type
            ConstructorInfo ci = typeTo.GetConstructor(new Type[] { nnTypeTo });
            ilg.Emit(OpCodes.Newobj, ci);
            ilg.Emit(OpCodes.Stloc, locTo);
            Label labEnd = ilg.DefineLabel();
            ilg.Emit(OpCodes.Br_S, labEnd);
            // if null then create a default one
            ilg.MarkLabel(labIfNull);
            ilg.Emit(OpCodes.Ldloca, locTo);
            ilg.Emit(OpCodes.Initobj, typeTo);
            ilg.MarkLabel(labEnd);
            ilg.Emit(OpCodes.Ldloc, locTo);
        }

        private static void EmitNullableToNonNullableStructConversion(ILGenerator ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            LocalBuilder locFrom = ilg.DeclareLocal(typeFrom);

            ilg.Emit(OpCodes.Stloc, locFrom);
            ilg.Emit(OpCodes.Ldloca, locFrom);
            EmitGetValue(ilg, typeFrom);
            Type nnTypeFrom = Nullable.GetUnderlyingType(typeFrom);
            EmitConvertToType(ilg, nnTypeFrom, typeTo, isChecked);
        }

        private static void EmitNullableToReferenceConversion(ILGenerator ilg, Type typeFrom)
        {
            ilg.Emit(OpCodes.Box, typeFrom);
        }

        private static void EmitNullableToNonNullableConversion(ILGenerator ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            if (typeTo.IsValueType)
            {
                EmitNullableToNonNullableStructConversion(ilg, typeFrom, typeTo, isChecked);
            }
            else
            {
                EmitNullableToReferenceConversion(ilg, typeFrom);
            }
        }

        private static void EmitNullableConversion(ILGenerator ilg, Type typeFrom, Type typeTo, bool isChecked)
        {
            bool isTypeFromNullable = typeFrom.IsNullable();
            bool isTypeToNullable = typeTo.IsNullable();

            if (isTypeFromNullable && isTypeToNullable)
            {
                EmitNullableToNullableConversion(ilg, typeFrom, typeTo, isChecked);
            }
            else if (isTypeFromNullable)
            {
                EmitNullableToNonNullableConversion(ilg, typeFrom, typeTo, isChecked);
            }
            else
            {
                EmitNonNullableToNullableConversion(ilg, typeFrom, typeTo, isChecked);
            }
        }

        #endregion

        #region Constants

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitString(ILGenerator ilg, string value)
        {
            ilg.Emit(OpCodes.Ldstr, value);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitBoolean(ILGenerator ilg, bool value)
        {
            if (value)
            {
                ilg.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                ilg.Emit(OpCodes.Ldc_I4_0);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitChar(ILGenerator ilg, char value)
        {
            EmitInt(ilg, value);
            ilg.Emit(OpCodes.Conv_U2);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitByte(ILGenerator ilg, byte value)
        {
            EmitInt(ilg, value);
            ilg.Emit(OpCodes.Conv_U1);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitSByte(ILGenerator ilg, sbyte value)
        {
            EmitInt(ilg, value);
            ilg.Emit(OpCodes.Conv_I1);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitShort(ILGenerator ilg, short value)
        {
            EmitInt(ilg, value);
            ilg.Emit(OpCodes.Conv_I2);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitUShort(ILGenerator ilg, ushort value)
        {
            EmitInt(ilg, value);
            ilg.Emit(OpCodes.Conv_U2);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitInt(ILGenerator ilg, int value)
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
                    if (value >= -128 && value <= 127)
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
        /// <inheritdoc/>
        /// </summary>
        public static void EmitUInt(ILGenerator ilg, uint value)
        {
            EmitInt(ilg, (int)value);
            ilg.Emit(OpCodes.Conv_U4);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitLong(ILGenerator ilg, long value)
        {
            ilg.Emit(OpCodes.Ldc_I8, value);

            //
            // Now, emit convert to give the constant type information.
            //
            // Otherwise, it is treated as unsigned and overflow is not
            // detected if it's used in checked ops.
            //
            ilg.Emit(OpCodes.Conv_I8);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitULong(ILGenerator ilg, ulong value)
        {
            ilg.Emit(OpCodes.Ldc_I8, (long)value);
            ilg.Emit(OpCodes.Conv_U8);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitDouble(ILGenerator ilg, double value)
        {
            ilg.Emit(OpCodes.Ldc_R8, value);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitSingle(ILGenerator ilg, float value)
        {
            ilg.Emit(OpCodes.Ldc_R4, value);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitDecimal(ILGenerator ilg, decimal value)
        {
            if (decimal.Truncate(value) == value)
            {
                if (int.MinValue <= value && value <= int.MaxValue)
                {
                    int intValue = decimal.ToInt32(value);
                    EmitInt(ilg, intValue);
                    EmitNew(ilg, typeof(decimal).GetConstructor(new Type[] { typeof(int) }));
                }
                else if (long.MinValue <= value && value <= long.MaxValue)
                {
                    long longValue = decimal.ToInt64(value);
                    EmitLong(ilg, longValue);
                    EmitNew(ilg, typeof(decimal).GetConstructor(new Type[] { typeof(long) }));
                }
                else
                {
                    EmitDecimalBits(ilg, value);
                }
            }
            else
            {
                EmitDecimalBits(ilg, value);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitDecimalBits(ILGenerator ilg, decimal value)
        {
            int[] bits = decimal.GetBits(value);
            EmitInt(ilg, bits[0]);
            EmitInt(ilg, bits[1]);
            EmitInt(ilg, bits[2]);
            EmitBoolean(ilg, (bits[3] & 0x80000000) != 0);
            EmitByte(ilg, (byte)(bits[3] >> 16));
            EmitNew(ilg, typeof(decimal).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) }));
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public static void EmitNew(ILGenerator ilg, ConstructorInfo ci)
        {
            if (ci.DeclaringType.ContainsGenericParameters)
            {
                throw new NotSupportedException();
            }

            ilg.Emit(OpCodes.Newobj, ci);
        }

        private static bool TryEmitILConstant(ILGenerator ilg, object value, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    EmitBoolean(ilg, (bool)value);
                    return true;
                case TypeCode.SByte:
                    EmitSByte(ilg, (sbyte)value);
                    return true;
                case TypeCode.Int16:
                    EmitShort(ilg, (short)value);
                    return true;
                case TypeCode.Int32:
                    EmitInt(ilg, (int)value);
                    return true;
                case TypeCode.Int64:
                    EmitLong(ilg, (long)value);
                    return true;
                case TypeCode.Single:
                    EmitSingle(ilg, (float)value);
                    return true;
                case TypeCode.Double:
                    EmitDouble(ilg, (double)value);
                    return true;
                case TypeCode.Char:
                    EmitChar(ilg, (char)value);
                    return true;
                case TypeCode.Byte:
                    EmitByte(ilg, (byte)value);
                    return true;
                case TypeCode.UInt16:
                    EmitUShort(ilg, (ushort)value);
                    return true;
                case TypeCode.UInt32:
                    EmitUInt(ilg, (uint)value);
                    return true;
                case TypeCode.UInt64:
                    EmitULong(ilg, (ulong)value);
                    return true;
                case TypeCode.Decimal:
                    EmitDecimal(ilg, (decimal)value);
                    return true;
                case TypeCode.String:
                    EmitString(ilg, (string)value);
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsSimpleArray(Type type)
        {
            if (!type.IsArray || type.GetArrayRank() > 1)
            {
                return false;
            }

            var elementType = type.GetElementType();

            return elementType.IsPrimitive || elementType == typeof(string) || elementType == typeof(Type) || elementType.IsSubclassOf(typeof(Type));
        }
        #endregion

        /// <summary>
        /// 设置指定类型的常量。
        /// </summary>
        /// <param name="defaultValue">默认值。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public static object SetConstantOfType(object defaultValue, Type conversionType)
        {
            if (defaultValue is null)
            {
                if (!conversionType.IsValueType || conversionType.IsNullable())
                {
                    return defaultValue;
                }

                throw new NotSupportedException($"默认值为“null”,不能作为“{conversionType}”的默认值!");
            }

            var valueType = defaultValue.GetType();

            if (valueType == conversionType)
            {
                return defaultValue;
            }

            if (conversionType.IsNullable())
            {
                conversionType = Nullable.GetUnderlyingType(conversionType);
            }

            if (valueType == conversionType)
            {
                return defaultValue;
            }

            return Convert.ChangeType(defaultValue, conversionType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 发行常量。
        /// </summary>
        public static void EmitConstantOfType(ILGenerator ilg, object value, Type valueType)
        {
            if (value is null)
            {
                ilg.Emit(OpCodes.Ldnull);
            }
            else
            {
                if (valueType is null)
                {
                    valueType = value.GetType();
                }

                switch (value)
                {
                    case Type type:
                        ilg.Emit(OpCodes.Ldtoken, type);
                        ilg.Emit(OpCodes.Call, GetTypeFromHandle);

                        ilg.Emit(OpCodes.Castclass, valueType);
                        break;
                    case MethodInfo method:
                        Debug.Assert(method.DeclaringType != null);

                        ilg.Emit(OpCodes.Ldtoken, method);

                        if (method is DynamicMethod dynamicMethod)
                        {
                            ilg.Emit(OpCodes.Ldtoken, dynamicMethod.DynamicDeclaringType);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Ldtoken, method.DeclaringType);
                        }
                        ilg.Emit(OpCodes.Call, GetIsGenericTypeMethodFromHandle);

                        ilg.Emit(OpCodes.Castclass, valueType);
                        break;
                    case Guid guid:
                        ilg.Emit(OpCodes.Ldstr, guid.ToString("D"));
                        ilg.Emit(OpCodes.Newobj, GuidConstructorInfo);
                        break;
                    case Array array:
                        if (!IsSimpleArray(valueType))
                        {
                            throw new NotSupportedException();
                        }

                        var elementType = valueType.GetElementType();

                        EmitInt(ilg, array.Length);

                        ilg.Emit(OpCodes.Newarr, elementType);

                        int index = 0;

                        var enumerator = array.GetEnumerator();

                        while (enumerator.MoveNext())
                        {
                            ilg.Emit(OpCodes.Dup);

                            EmitInt(ilg, index++);

                            EmitConstantOfType(ilg, enumerator.Current, elementType);

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

                        break;
                    default:
                        if (TryEmitILConstant(ilg, value, valueType))
                        {
                            return;
                        }

                        if (value.GetType() != valueType)
                        {
                            value = Convert.ChangeType(value, valueType);
                        }

                        if (!ConstantCache.TryGetValue(value, out int key))
                        {
                            lock (ConstantCache)
                            {
                                if (!ConstantCache.TryGetValue(value, out key))
                                {
                                    ConstantCache.Add(value, key = Constants.Count);

                                    Constants.Add(value);
                                }
                            }
                        }

                        EmitInt(ilg, key);

                        ilg.Emit(OpCodes.Call, GetConstantMethod);

                        if (valueType.IsValueType)
                        {
                            ilg.Emit(OpCodes.Unbox_Any, valueType);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Castclass, valueType);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// 类型转换。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="typeFrom">源类型。</param>
        /// <param name="typeTo">目标类型。</param>
        /// <param name="isChecked">类型检查。</param>
        public static void EmitConvertToType(ILGenerator ilg, Type typeFrom, Type typeTo, bool isChecked = true)
        {
            if (AreEquivalent(typeFrom, typeTo))
            {
                return;
            }

            bool isTypeFromNullable = typeFrom.IsNullable();
            bool isTypeToNullable = typeTo.IsNullable();

            Type nnExprType = typeFrom.IsNullable() ? Nullable.GetUnderlyingType(typeFrom) : typeFrom;
            Type nnType = typeTo.IsNullable() ? Nullable.GetUnderlyingType(typeTo) : typeTo;

            if (typeFrom.IsInterface || // interface cast
               typeTo.IsInterface ||
               typeFrom == typeof(object) || // boxing cast
               typeTo == typeof(object) ||
               typeFrom == typeof(Enum) ||
               typeFrom == typeof(ValueType) ||
               IsLegalExplicitVariantDelegateConversion(typeFrom, typeTo))
            {
                EmitCastToType(ilg, typeFrom, typeTo);
            }
            else if (isTypeFromNullable || isTypeToNullable)
            {
                EmitNullableConversion(ilg, typeFrom, typeTo, isChecked);
            }
            else if (!(IsConvertible(typeFrom) && IsConvertible(typeTo)) && (nnExprType.IsAssignableFrom(nnType) || nnType.IsAssignableFrom(nnExprType)))
            {
                EmitCastToType(ilg, typeFrom, typeTo);
            }
            else if (typeFrom.IsArray && typeTo.IsArray)
            {
                // See DevDiv Bugs #94657.
                EmitCastToType(ilg, typeFrom, typeTo);
            }
            else
            {
                EmitNumericConversion(ilg, typeFrom, typeTo, isChecked);
            }
        }

        #region Attribute
        /// <summary>
        /// 创建自定义标记。
        /// </summary>
        /// <typeparam name="TAttribute">标记类型。</typeparam>
        /// <returns></returns>
        public static CustomAttributeBuilder CreateCustomAttribute<TAttribute>() where TAttribute : Attribute, new() => new CustomAttributeBuilder(typeof(TAttribute).GetConstructor(Type.EmptyTypes), new object[0]);


        /// <summary>
        /// 创建自定义属性构造器。
        /// </summary>
        /// <param name="attribute">属性。</param>
        /// <returns></returns>
        public static CustomAttributeBuilder CreateCustomAttribute(CustomAttributeData attribute)
        {
            if (attribute is null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var constructorArguments = GetArguments(attribute.ConstructorArguments);

#if NET40            
            var namedArguments = GetSettersAndFields(attribute.Constructor.DeclaringType, attribute.NamedArguments);

            return new CustomAttributeBuilder(attribute.Constructor.DeclaringType.GetConstructor(constructorArguments.Item1), constructorArguments.Item2, namedArguments.Item1, namedArguments.Item2, namedArguments.Item3, namedArguments.Item4);
#else
            var namedArguments = GetSettersAndFields(attribute.AttributeType, attribute.NamedArguments);

            return new CustomAttributeBuilder(attribute.AttributeType.GetConstructor(constructorArguments.Item1), constructorArguments.Item2, namedArguments.Item1, namedArguments.Item2, namedArguments.Item3, namedArguments.Item4);
#endif
        }

        private static Tuple<Type[], object[]> GetArguments(IList<CustomAttributeTypedArgument> constructorArguments)
        {
            var constructorArgTypes = new Type[constructorArguments.Count];
            var constructorArgs = new object[constructorArguments.Count];
            for (var i = 0; i < constructorArguments.Count; i++)
            {
                constructorArgTypes[i] = constructorArguments[i].ArgumentType;
                constructorArgs[i] = ReadAttributeValue(constructorArguments[i]);
            }

            return Tuple.Create(constructorArgTypes, constructorArgs);
        }

        private static Tuple<PropertyInfo[], object[], FieldInfo[], object[]> GetSettersAndFields(Type attributeType, IEnumerable<CustomAttributeNamedArgument> namedArguments)
        {
            var propertyList = new List<PropertyInfo>();
            var propertyValuesList = new List<object>();
            var fieldList = new List<FieldInfo>();
            var fieldValuesList = new List<object>();
            foreach (var argument in namedArguments)
            {
#if NET40
                if (argument.MemberInfo is FieldInfo)
                {
                    fieldList.Add(attributeType.GetField(argument.MemberInfo.Name));
                    fieldValuesList.Add(ReadAttributeValue(argument.TypedValue));
                }
                else
                {
                    propertyList.Add(attributeType.GetProperty(argument.MemberInfo.Name));
                    propertyValuesList.Add(ReadAttributeValue(argument.TypedValue));
                }
#else
                if (argument.IsField)
                {
                    fieldList.Add(attributeType.GetField(argument.MemberName));
                    fieldValuesList.Add(ReadAttributeValue(argument.TypedValue));
                }
                else
                {
                    propertyList.Add(attributeType.GetProperty(argument.MemberName));
                    propertyValuesList.Add(ReadAttributeValue(argument.TypedValue));
                }
#endif
            }

            return Tuple.Create(propertyList.ToArray(), propertyValuesList.ToArray(), fieldList.ToArray(), fieldValuesList.ToArray());
        }

        private static object ReadAttributeValue(CustomAttributeTypedArgument argument)
        {
            var value = argument.Value;
            if (argument.ArgumentType.IsArray && value is IList<CustomAttributeTypedArgument> arrays)
            {
                //special case for handling arrays in attributes
                return GetNestedArguments(arrays);
            }

            return value;
        }

        private static object[] GetNestedArguments(IList<CustomAttributeTypedArgument> constructorArguments)
        {
            var arguments = new object[constructorArguments.Count];

            for (var i = 0; i < constructorArguments.Count; i++)
            {
                arguments[i] = ReadAttributeValue(constructorArguments[i]);
            }

            return arguments;
        }
        #endregion
    }
}

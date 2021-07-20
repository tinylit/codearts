using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 默认值。
    /// </summary>
    [DebuggerDisplay("default({RuntimeType.Name})")]
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
            if (RuntimeType.IsByRef)
            {
                EmitByRef(ilg, RuntimeType.GetElementType());
            }
            else
            {
                Emit(ilg, RuntimeType);
            }
        }

        private static void Emit(ILGenerator ilg, Type type)
        {
            if (type.IsValueType && type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type.IsPrimitive && type != typeof(IntPtr) && type != typeof(UIntPtr))
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.DBNull:
                        ilg.Emit(OpCodes.Ldsfld, typeof(DBNull).GetField(nameof(DBNull.Value)));
                        break;
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case TypeCode.Char:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        ilg.Emit(OpCodes.Ldc_I8, 0L);
                        break;

                    case TypeCode.Single:
                        ilg.Emit(OpCodes.Ldc_R4, 0F);
                        break;

                    case TypeCode.Double:
                        ilg.Emit(OpCodes.Ldc_R8, 0D);
                        break;

                    case TypeCode.Decimal:
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        ilg.Emit(OpCodes.Newobj, typeof(decimal).GetConstructor(new Type[] { typeof(int) }));
                        break;

                    case TypeCode.Empty:
                    case TypeCode.String:
                    case TypeCode.Object:
                    case TypeCode.DateTime:
                    default:
                        if (type.IsValueType)
                        {
                            var local = ilg.DeclareLocal(type);
                            ilg.Emit(OpCodes.Ldloca_S, local);
                            ilg.Emit(OpCodes.Initobj, type);
                            ilg.Emit(OpCodes.Ldloc, local);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Ldnull);
                        }
                        break;
                }
            }
            else if (type.IsValueType || type.IsGenericParameter)
            {
                var local = ilg.DeclareLocal(type);
                ilg.Emit(OpCodes.Ldloca_S, local);
                ilg.Emit(OpCodes.Initobj, type);
                ilg.Emit(OpCodes.Ldloc, local);
            }
            else
            {
                ilg.Emit(OpCodes.Ldnull);
            }
        }

        private static void EmitByRef(ILGenerator ilg, Type type)
        {
            if (type.IsValueType && type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
            }

            if (type.IsPrimitive && type != typeof(IntPtr) && type != typeof(UIntPtr))
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.DBNull:
                        ilg.Emit(OpCodes.Ldsfld, typeof(DBNull).GetField(nameof(DBNull.Value)));

                        ilg.Emit(OpCodes.Stobj, type);
                        break;
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        ilg.Emit(OpCodes.Stind_I1);
                        break;
                    case TypeCode.Char:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        ilg.Emit(OpCodes.Stind_I2);
                        break;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        ilg.Emit(OpCodes.Stind_I4);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        ilg.Emit(OpCodes.Ldc_I8, 0L);
                        ilg.Emit(OpCodes.Stind_I8);
                        break;

                    case TypeCode.Single:
                        ilg.Emit(OpCodes.Ldc_R4, 0F);
                        ilg.Emit(OpCodes.Stind_R4);
                        break;

                    case TypeCode.Double:
                        ilg.Emit(OpCodes.Ldc_R8, 0D);
                        ilg.Emit(OpCodes.Stind_R8);
                        break;

                    case TypeCode.Decimal:
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        ilg.Emit(OpCodes.Newobj, typeof(decimal).GetConstructor(new Type[] { typeof(int) }));

                        ilg.Emit(OpCodes.Stobj, type);
                        break;

                    case TypeCode.Empty:
                    case TypeCode.String:
                    case TypeCode.Object:
                    case TypeCode.DateTime:
                    default:
                        if (type.IsValueType)
                        {
                            ilg.Emit(OpCodes.Initobj, type);

                            ilg.Emit(OpCodes.Stobj, type);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Ldnull);

                            ilg.Emit(OpCodes.Stind_Ref);
                        }
                        break;
                }
            }
            else if (type.IsValueType || type.IsGenericParameter)
            {
                ilg.Emit(OpCodes.Initobj, type);
            }
            else
            {
                ilg.Emit(OpCodes.Ldnull);

                ilg.Emit(OpCodes.Stind_Ref);
            }
        }
    }
}

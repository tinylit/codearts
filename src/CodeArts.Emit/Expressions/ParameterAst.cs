using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 参数。
    /// </summary>
    [DebuggerDisplay("{RuntimeType.Name} ({position})")]
    public class ParameterAst : AstExpression
    {
        private readonly bool canWrite = true;

        /// <summary>
        /// 参数位置。
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// 可写。
        /// </summary>
        public override bool CanWrite => canWrite;

        /// <summary>
        /// 获取一个值，该值指示 System.Type 是否由引用传递。
        /// </summary>
        public virtual bool IsByRef => RuntimeType.IsByRef;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="paramterType">参数类型。</param>
        /// <param name="position">参数位置。</param>
        public ParameterAst(Type paramterType, int position) : base(paramterType)
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "参数位置不能小于零。");
            }

            Position = position;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="parameter">参数。</param>
        public ParameterAst(ParameterInfo parameter) : this(parameter.ParameterType, parameter.Position + 1)
        {
            canWrite = !IsReadOnly(parameter);
        }

        private static bool IsReadOnly(ParameterInfo parameter)
        {
            if ((parameter.Attributes & (ParameterAttributes.In | ParameterAttributes.Out)) != ParameterAttributes.In)
            {
                return false;
            }

            if (parameter.GetRequiredCustomModifiers().Any(x => x == typeof(InAttribute)))
            {
                return true;
            }

            if (parameter.GetCustomAttributes(false).Any(x => x.GetType().FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            switch (Position)
            {
                case 0:
                    ilg.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    ilg.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    ilg.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    ilg.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (Position < byte.MaxValue)
                    {
                        ilg.Emit(OpCodes.Ldarg_S, (byte)Position);

                        break;
                    }

                    ilg.Emit(OpCodes.Ldarg, Position);
                    break;
            }

            if (IsByRef)
            {
                var type = RuntimeType.GetElementType();

                if (type.IsValueType && type.IsEnum)
                {
                    type = Enum.GetUnderlyingType(type);
                }

                if (type.IsPrimitive && type != typeof(IntPtr) && type != typeof(UIntPtr))
                {
                    switch (Type.GetTypeCode(type))
                    {
                        case TypeCode.Boolean:
                        case TypeCode.Byte:
                        case TypeCode.SByte:
                            ilg.Emit(OpCodes.Ldind_I1);
                            break;
                        case TypeCode.Char:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                            ilg.Emit(OpCodes.Ldind_I2);
                            break;
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                            ilg.Emit(OpCodes.Ldind_I4);
                            break;

                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                            ilg.Emit(OpCodes.Ldind_I8);
                            break;

                        case TypeCode.Single:
                            ilg.Emit(OpCodes.Ldind_R4);
                            break;

                        case TypeCode.Double:
                            ilg.Emit(OpCodes.Ldind_R8);
                            break;
                        case TypeCode.Empty:
                        case TypeCode.DBNull:
                        case TypeCode.Decimal:
                        case TypeCode.DateTime:
                        case TypeCode.String:
                        case TypeCode.Object:
                        default:
                            if (type.IsValueType)
                            {
                                ilg.Emit(OpCodes.Ldobj, type);
                            }
                            else
                            {
                                ilg.Emit(OpCodes.Ldind_Ref);
                            }
                            break;
                    }
                }
                else if (type.IsValueType || type.IsGenericParameter)
                {
                    ilg.Emit(OpCodes.Ldobj, type);
                }
                else
                {
                    ilg.Emit(OpCodes.Ldind_Ref);
                }
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void AssignCore(ILGenerator ilg, AstExpression value)
        {

            if (IsByRef)
            {
                switch (Position)
                {
                    case 0:
                        ilg.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        ilg.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        ilg.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        ilg.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        if (Position < byte.MaxValue)
                        {
                            ilg.Emit(OpCodes.Ldarg_S, (byte)Position);

                            break;
                        }

                        ilg.Emit(OpCodes.Ldarg, Position);
                        break;
                }
            }
            else if (Position < byte.MaxValue)
            {
                ilg.Emit(OpCodes.Starg_S, (byte)Position);
            }
            else
            {
                ilg.Emit(OpCodes.Starg, Position);
            }

            value.Load(ilg);

            if (IsByRef)
            {
                var type = RuntimeType.GetElementType();

                if (type.IsValueType && type.IsEnum)
                {
                    type = Enum.GetUnderlyingType(type);
                }

                if (type.IsPrimitive)
                {
                    switch (Type.GetTypeCode(type))
                    {
                        case TypeCode.Boolean:
                        case TypeCode.Byte:
                        case TypeCode.SByte:
                            ilg.Emit(OpCodes.Stind_I1);
                            break;
                        case TypeCode.Char:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                            ilg.Emit(OpCodes.Stind_I2);
                            break;
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                            ilg.Emit(OpCodes.Stind_I4);
                            break;

                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                            ilg.Emit(OpCodes.Stind_I8);
                            break;

                        case TypeCode.Single:
                            ilg.Emit(OpCodes.Stind_R4);
                            break;

                        case TypeCode.Double:
                            ilg.Emit(OpCodes.Stind_R8);
                            break;
                        case TypeCode.Empty:
                        case TypeCode.DBNull:
                        case TypeCode.Decimal:
                        case TypeCode.DateTime:
                        case TypeCode.String:
                        case TypeCode.Object:
                        default:
                            if (type.IsValueType)
                            {
                                ilg.Emit(OpCodes.Stobj, type);
                            }
                            else
                            {
                                ilg.Emit(OpCodes.Stind_Ref);
                            }
                            break;
                    }
                }
                else if (type.IsValueType)
                {
                    ilg.Emit(OpCodes.Stobj, type);
                }
                else if (type.IsGenericParameter)
                {
                    ilg.Emit(OpCodes.Stobj, type);
                }
                else
                {
                    ilg.Emit(OpCodes.Stind_Ref);
                }
            }
        }
    }
}

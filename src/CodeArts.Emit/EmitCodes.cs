﻿using CodeArts.Emit.Expressions;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CodeArts.Emit
{
    /// <summary>
    /// 工具
    /// </summary>
    public class EmitCodes : DesignMode.Singleton<EmitCodes>
    {
        private static readonly OpCode emptyOpCode = new OpCode();

        private readonly Dictionary<Type, OpCode> codeCache;

        private EmitCodes()
        {
            codeCache = new Dictionary<Type, OpCode>
                {
                    { typeof(bool), OpCodes.Ldc_I4 },
                    { typeof(char), OpCodes.Ldc_I4 },
                    { typeof(sbyte), OpCodes.Ldc_I4 },
                    { typeof(short), OpCodes.Ldc_I4 },
                    { typeof(int), OpCodes.Ldc_I4 },
                    { typeof(long), OpCodes.Ldc_I8 },
                    { typeof(float), OpCodes.Ldc_R4 },
                    { typeof(double), OpCodes.Ldc_R8 },
                    { typeof(byte), OpCodes.Ldc_I4_0 },
                    { typeof(ushort), OpCodes.Ldc_I4_0 },
                    { typeof(uint), OpCodes.Ldc_I4_0 },
                    { typeof(ulong), OpCodes.Ldc_I4_0 }
                };
        }

        /// <summary>
        /// 索引。
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public OpCode this[Type type]
        {
            get
            {
                if (codeCache.TryGetValue(type, out OpCode opCode))
                {
                    return opCode;
                }

                return emptyOpCode;
            }
            set
            {
                codeCache[type] = value;
            }
        }

        /// <summary>
        /// 发行指定类型的默认值。
        /// </summary>
        /// <param name="ilg">指令</param>
        /// <param name="type">类型</param>
        public static void EmitDefaultValueOfType(ILGenerator ilg, Type type)
        {
            if (type.IsPrimitive)
            {
                var opCode = Instance[type];

                switch (opCode.StackBehaviourPush)
                {
                    case StackBehaviour.Pushi:

                        ilg.Emit(opCode, 0);

                        if (type == typeof(long) || type == typeof(ulong))
                        {
                            ilg.Emit(OpCodes.Conv_I8);
                        }
                        break;
                    case StackBehaviour.Pushr8:
                        ilg.Emit(opCode, 0D);
                        break;
                    case StackBehaviour.Pushi8:
                        ilg.Emit(opCode, 0L);
                        break;
                    case StackBehaviour.Pushr4:
                        ilg.Emit(opCode, 0F);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                ilg.Emit(OpCodes.Ldnull);
            }
        }

        /// <summary>
        /// 发行转存类型。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="type">类型。</param>
        public static void EmitAssignIndirectOpCodeForType(ILGenerator ilg, Type type)
        {
            if (type.IsEnum)
            {
                EmitAssignIndirectOpCodeForType(ilg, Enum.GetUnderlyingType(type));
                return;
            }

            if (type.IsByRef)
            {
                throw new NotSupportedException("Cannot store ByRef values");
            }
            else if (type.IsPrimitive && type != typeof(IntPtr) && type != typeof(UIntPtr))
            {
                var opCode = Instance[type];

                if (opCode == emptyOpCode)
                {
                    throw new ArgumentException("Type " + type + " could not be converted to a OpCode");
                }

                ilg.Emit(opCode);
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

        /// <summary>
        /// 发行引用链。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="expression">成员。</param>
        public static void EmitLoad(ILGenerator ilg, Expression expression)
        {
            if (expression is null)
            {
                return;
            }

            if (expression is MemberExpression member)
            {
                if (member.Expression == expression)
                {
                    return;
                }

                EmitLoad(ilg, expression);
            }

            expression.Emit(ilg);
        }
    }
}

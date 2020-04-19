using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 类型转换。
    /// </summary>
    [DebuggerDisplay("({type.Name}){expression}")]
    public class ConvertExpression : Expression
    {
        private readonly Type type;
        private readonly Expression expression;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="expression">表达式</param>
        /// <param name="type">转换类型</param>
        public ConvertExpression(Expression expression, Type type) : base(type)
        {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令</param>
        public override void Emit(ILGenerator ilg)
        {
            expression.Emit(ilg);

            Type fromType = expression.ReturnType;

            if (fromType == type)
            {
                return;
            }

            Type targetType = type;

            if (type.IsByRef)
            {
                targetType = type.GetElementType();
            }

            if (type.IsValueType)
            {
                if (fromType.IsValueType)
                {
                    throw new EmitException("不支持值类型和值类型的转换!");
                }

                var code = EmitCodes.Instance[targetType];

                if (code == default)
                {
                    ilg.Emit(OpCodes.Unbox_Any, targetType);
                }
                else
                {
                    ilg.Emit(OpCodes.Unbox, targetType);

                    Emit(ilg, targetType);
                }
            }
            else if (fromType.IsValueType)
            {
                ilg.Emit(OpCodes.Box, fromType);

                Emit(ilg, typeof(object), targetType);
            }
            else
            {
                Emit(ilg, fromType, targetType);
            }
        }

        private static void Emit(ILGenerator gen, Type type)
        {
            if (type.IsEnum)
            {
                Emit(gen, Enum.GetUnderlyingType(type));

                return;
            }

            if (type.IsByRef)
            {
                throw new NotSupportedException("Cannot load ByRef values");
            }

            if (type.IsPrimitive && type != typeof(IntPtr) && type != typeof(UIntPtr))
            {
                gen.Emit(EmitCodes.Instance[type]);
            }
            else if (type.IsValueType)
            {
                gen.Emit(OpCodes.Ldobj, type);
            }
            else if (type.IsGenericParameter)
            {
                gen.Emit(OpCodes.Ldobj, type);
            }
            else
            {
                gen.Emit(OpCodes.Ldind_Ref);
            }
        }

        private static void Emit(ILGenerator gen, Type fromType, Type targetType)
        {
            if (targetType.IsGenericParameter)
            {
                gen.Emit(OpCodes.Unbox_Any, targetType);
            }
            else if (fromType.IsGenericParameter)
            {
                gen.Emit(OpCodes.Box, fromType);
            }
            else if (targetType.IsGenericType && targetType != fromType || targetType.IsSubclassOf(fromType))
            {
                gen.Emit(OpCodes.Castclass, targetType);
            }
        }
    }
}

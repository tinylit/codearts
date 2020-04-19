using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeArts.Emit.Expressions
{
    /// <summary>
    /// 是否为指定类型。
    /// </summary>
    [DebuggerDisplay("{body} is {isType}")]
    public class TypeIsExpression : Expression
    {
        private enum AnalyzeTypeIsResult
        {
            KnownFalse,
            KnownTrue,
            KnownAssignable, // need null check only
            Unknown,         // need full runtime check
        }

        private readonly Expression body;
        private readonly Type isType;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">成员</param>
        /// <param name="isType">类型</param>
        public TypeIsExpression(Expression body, Type isType) : base(typeof(bool))
        {
            this.body = body;
            this.isType = isType;
        }

        /// <summary>
        /// 取值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Emit(ILGenerator ilg)
        {
            var type = body.ReturnType;

            AnalyzeTypeIsResult result = AnalyzeTypeIs(type, isType);

            if (result == AnalyzeTypeIsResult.KnownTrue ||
                result == AnalyzeTypeIsResult.KnownFalse)
            {
                if (result == AnalyzeTypeIsResult.KnownTrue)
                {
                    ilg.Emit(OpCodes.Ldc_I4_1);
                }
                else
                {
                    ilg.Emit(OpCodes.Ldc_I4_0);
                }

                return;
            }

            if (result == AnalyzeTypeIsResult.KnownAssignable)
            {
                if (type.IsNullable())
                {
                    if (body is MemberExpression member)
                    {
                        EmitCodes.EmitLoad(ilg, member.Expression);
                        MethodInfo mi = type.GetMethod("get_HasValue", BindingFlags.Instance | BindingFlags.Public);
                        ilg.Emit(OpCodes.Call, mi);

                        return;
                    }

                    throw new NotSupportedException();
                }

                body.Emit(ilg);

                ilg.Emit(OpCodes.Ldnull);
                ilg.Emit(OpCodes.Ceq);
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Ceq);

                return;
            }

            body.Emit(ilg);

            if (type.IsValueType)
            {
                ilg.Emit(OpCodes.Box, type);
            }

            ilg.Emit(OpCodes.Isinst, isType);
            ilg.Emit(OpCodes.Ldnull);
            ilg.Emit(OpCodes.Cgt_Un);
        }

        private static AnalyzeTypeIsResult AnalyzeTypeIs(Type operandType, Type testType)
        {
            if (operandType == typeof(void))
            {
                return AnalyzeTypeIsResult.KnownFalse;
            }

            Type nnOperandType = operandType.IsNullable() ? Nullable.GetUnderlyingType(operandType) : operandType;
            Type nnTestType = testType.IsNullable() ? Nullable.GetUnderlyingType(testType) : testType;

            if (nnTestType.IsAssignableFrom(nnOperandType))
            {
                if (operandType.IsValueType && !operandType.IsNullable())
                {
                    return AnalyzeTypeIsResult.KnownTrue;
                }

                return AnalyzeTypeIsResult.KnownAssignable;
            }

            return AnalyzeTypeIsResult.Unknown;
        }
    }
}

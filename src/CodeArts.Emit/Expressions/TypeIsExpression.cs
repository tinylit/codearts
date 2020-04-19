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
        /// <param name="iLGen">指令。</param>
        public override void Emit(ILGenerator iLGen)
        {
            var type = body.ReturnType;

            AnalyzeTypeIsResult result = AnalyzeTypeIs(type, isType);

            if (result == AnalyzeTypeIsResult.KnownTrue ||
                result == AnalyzeTypeIsResult.KnownFalse)
            {
                if (result == AnalyzeTypeIsResult.KnownTrue)
                {
                    iLGen.Emit(OpCodes.Ldc_I4_1);
                }
                else
                {
                    iLGen.Emit(OpCodes.Ldc_I4_0);
                }

                return;
            }

            if (result == AnalyzeTypeIsResult.KnownAssignable)
            {
                if (type.IsNullable())
                {
                    if (body is MemberExpression member)
                    {
                        EmitCodes.EmitLoad(iLGen, member.Expression);
                        MethodInfo mi = type.GetMethod("get_HasValue", BindingFlags.Instance | BindingFlags.Public);
                        iLGen.Emit(OpCodes.Call, mi);

                        return;
                    }

                    throw new NotSupportedException();
                }

                body.Emit(iLGen);

                iLGen.Emit(OpCodes.Ldnull);
                iLGen.Emit(OpCodes.Ceq);
                iLGen.Emit(OpCodes.Ldc_I4_0);
                iLGen.Emit(OpCodes.Ceq);

                return;
            }

            body.Emit(iLGen);

            if (type.IsValueType)
            {
                iLGen.Emit(OpCodes.Box, type);
            }

            iLGen.Emit(OpCodes.Isinst, isType);
            iLGen.Emit(OpCodes.Ldnull);
            iLGen.Emit(OpCodes.Cgt_Un);
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

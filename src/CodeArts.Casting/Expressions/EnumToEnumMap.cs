using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Casting.Expressions
{
    using static Expression;

    /// <summary>
    /// 枚举转枚举。
    /// </summary>
    public class EnumToEnumMap : SimpleExpression
    {
        private readonly static List<Type> EnumTypes = new List<Type>(8)
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong)
        };

        private static readonly MethodInfo TryParseMethod = typeof(Enum).GetMember(nameof(Enum.TryParse), MemberTypes.Method, MapExtensions.StaticFlags & ~BindingFlags.NonPublic)
                                                            .Cast<MethodInfo>()
                                                            .First(x => x.IsGenericMethod && x.GetParameters().Length == 3);

        private static readonly MethodInfo ToStringMethod = typeof(Enum).GetMethod(nameof(Enum.ToString), Type.EmptyTypes);

        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type conversionType) => sourceType.IsEnum && conversionType.IsEnum;

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceType">原类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="sourceExpression">数据源表达式。</param>
        /// <returns></returns>
        protected override Expression ToSolve(Type sourceType, Type conversionType, Expression sourceExpression)
        {
            var variableExp = Variable(conversionType);

            var tryParseExp = Call(TryParseMethod.MakeGenericMethod(conversionType), Call(sourceExpression, ToStringMethod), Constant(true), variableExp);

            return Block(conversionType, new ParameterExpression[1] { variableExp }, Condition(tryParseExp, variableExp, Aw_ToSolve(sourceType, conversionType, sourceExpression)));
        }

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceType">原类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="sourceExpression">数据源表达式。</param>
        /// <returns></returns>
        private Expression Aw_ToSolve(Type sourceType, Type conversionType, Expression sourceExpression)
        {
            var sourceUnderlyingType = Enum.GetUnderlyingType(sourceType);
            var conversionUnderlyingType = Enum.GetUnderlyingType(conversionType);

            int indexOfSource = EnumTypes.IndexOf(sourceUnderlyingType);
            int indexOfDest = EnumTypes.IndexOf(conversionUnderlyingType);

            //? 目标类型值，大等于原类型值。
            if (indexOfSource <= indexOfDest)
            {
                int remainderSource = indexOfSource & 1;
                int remainderDest = indexOfDest & 1;

                if (remainderSource == remainderDest) //? 同是有符号或无符号。
                {
                    return Convert(sourceExpression, conversionType);
                }

                if (remainderSource == 0 && indexOfDest > indexOfSource + 1) //? 源是无符号，目标大一级；如：sbyte => short。
                {
                    return Convert(sourceExpression, conversionType);
                }

                //? 源有符号，目标类型为无符号。如:short, ushort
                return Condition(GreaterThan(sourceExpression, Constant(-1, sourceType)),
                         ThrowError(sourceExpression, sourceType, sourceType, conversionType),
                         Convert(sourceExpression, conversionType));
            }

            var maxValueField = conversionUnderlyingType.GetField(nameof(int.MaxValue));

            return Condition(GreaterThan(Convert(sourceExpression, sourceUnderlyingType), Constant(maxValueField.GetRawConstantValue(), sourceUnderlyingType)),
                    ThrowError(Convert(sourceExpression, sourceUnderlyingType), sourceUnderlyingType, sourceType, conversionType),
                    Convert(sourceExpression, conversionType));

        }

        private readonly static MethodInfo Concat = typeof(string).GetMethod("Concat", MapExtensions.StaticFlags, null, new Type[3] { typeof(string), typeof(string), typeof(string) }, null);
        private readonly static ConstructorInfo InvalidCastExceptionCtor = typeof(InvalidCastException).GetConstructor(new Type[1] { typeof(string) });
        private static Expression ThrowError(Expression variable, Type sourceUnderlyingType, Type sourceType, Type conversionType)
        {
            return Throw(New(InvalidCastExceptionCtor, Call(Concat, Constant($"无法将类型({sourceType})的值"), Call(variable, sourceUnderlyingType.GetMethod("ToString", Type.EmptyTypes)), Constant($"转换为类型({conversionType})!"))));
        }
    }
}

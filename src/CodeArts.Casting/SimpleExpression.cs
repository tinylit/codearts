using System;
using System.Linq.Expressions;

namespace CodeArts.Casting
{
    /// <summary>
    /// 简单表达式。
    /// </summary>
    public abstract class SimpleExpression : IMapExpression
    {
        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public abstract bool IsMatch(Type sourceType, Type conversionType);

        /// <summary>
        /// 解决。
        /// </summary>
        /// <typeparam name="TResult">结果类型。</typeparam>
        /// <param name="sourceType">原类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        public virtual Func<object, TResult> ToSolve<TResult>(Type sourceType, Type conversionType)
        {
            var parameterExp = Expression.Parameter(typeof(object));

            var variableExp = Expression.Variable(sourceType);

            var bodyExp = ToSolve(sourceType, conversionType, variableExp);

            var lambdaExp = Expression.Lambda<Func<object, TResult>>(Expression.Block(new ParameterExpression[1] { variableExp },
                    Expression.Assign(variableExp, Expression.Convert(parameterExp, sourceType)),
                    bodyExp),
                    new ParameterExpression[1] { parameterExp });

            return lambdaExp.Compile();
        }

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceType">原类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <param name="sourceExpression">数据源表达式。</param>
        /// <returns></returns>
        protected virtual Expression ToSolve(Type sourceType, Type conversionType, Expression sourceExpression) => throw new NotImplementedException();
    }
}

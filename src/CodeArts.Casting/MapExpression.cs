using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.Casting
{
    /// <summary>
    /// 映射。
    /// </summary>
    public abstract class MapExpression : IMapExpression
    {
        /// <summary>
        /// 能否解决。
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
        /// <param name="configuration">配置文件。</param>
        /// <returns></returns>
        public virtual Func<object, TResult> ToSolve<TResult>(Type sourceType, Type conversionType, IMapConfiguration configuration)
        {
            var parameterExp = Expression.Parameter(typeof(object));

            var variableExp = Expression.Variable(sourceType);

            var bodyExp = ToSolve(sourceType, conversionType, configuration, variableExp);

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
        /// <param name="configuration">配置文件。</param>
        /// <param name="sourceExpression">数据源表达式。</param>
        /// <returns></returns>
        protected virtual Expression ToSolve(Type sourceType, Type conversionType, IMapConfiguration configuration, Expression sourceExpression)
        {
            throw new NotImplementedException();
        }
    }
}

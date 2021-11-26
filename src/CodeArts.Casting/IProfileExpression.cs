using System;
using System.Collections.Generic;

namespace CodeArts.Casting
{
    /// <summary>
    /// 表达式配置。
    /// </summary>
    public interface IProfileExpression
    {
        /// <summary>
        /// 映射。
        /// </summary>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="predicate">判断源类型是否支持转到目标类型。</param>
        /// <param name="project">将对象转为目标类型的方案。</param>
        void Map<TResult>(Predicate<Type> predicate, Func<object, TResult> project);

        /// <summary>
        /// 映射。
        /// </summary>
        /// <typeparam name="TSource">源类型。</typeparam>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="project">将对象转为目标类型的方案。</param>
        void Absolute<TSource, TResult>(Func<TSource, TResult> project);

        /// <summary>
        /// 运行(目标类型和源类型相同，或目标类型继承或实现源类型)。
        /// </summary>
        /// <typeparam name="TSource">源数据类型。</typeparam>
        /// <typeparam name="TResult">目标类型。</typeparam>
        /// <param name="project">将源数据转为目标数据的方案。</param>
        void Run<TSource, TResult>(Func<TSource, TResult> project);
    }
}

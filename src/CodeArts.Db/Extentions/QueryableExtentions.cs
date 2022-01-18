using CodeArts.Db;
using System.Linq.Expressions;

namespace System.Linq
{
    /// <summary>
    /// 查询器扩展。
    /// </summary>
    public static class QueryableExtentions
    {
        /// <summary>
        /// 查询条件。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="condition">是否追加条件。</param>
        /// <param name="predicate">条件表达式。</param>
        /// <returns></returns>
        public static IQueryable<TSource> WhereIf<TSource>(this IQueryable<TSource> source, bool condition, Expression<Func<TSource, bool>> predicate)
        {
            if (condition)
            {
                return source.Where(predicate);
            }

            return source;
        }
    }
}

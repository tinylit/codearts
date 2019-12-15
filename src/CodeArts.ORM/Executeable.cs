using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CodeArts.ORM
{
    /// <summary>
    /// 执行能力
    /// </summary>
    public static class Executeable
    {
        private static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f, T1 _) => f.Method;
        private static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 _, T2 _2) => f.Method;

        /// <summary>
        /// 执行条件
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <param name="source">源</param>
        /// <param name="expression">条件表达式</param>
        /// <returns></returns>
        public static IExecuteable<T> Where<T>(this IExecuteable<T> source, Expression<Func<T, bool>> expression)
        => source.Provider.CreateExecute(Expression.Call(null, GetMethodInfo(Where, source, expression), new Expression[2] {
            source.Expression,
            Expression.Quote(expression)
        }));

        /// <summary>
        /// 执行数据表
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <param name="source">源</param>
        /// <param name="table">获取表名称的工厂</param>
        /// <returns></returns>
        public static IExecuteable<T> From<T>(this IExecuteable<T> source, Func<ITableRegions, string> table)
        => source.Provider.CreateExecute(Expression.Call(null, GetMethodInfo(From, source, table), new Expression[2] {
            source.Expression,
            Expression.Constant(table)
        }));

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <param name="source">源</param>
        /// <param name="updater">更新的字段和值</param>
        /// <returns></returns>
        public static int Update<T>(this IExecuteable<T> source, Expression<Func<T, T>> updater)
        => source.Provider.Execute(Expression.Call(null, GetMethodInfo(Update, source, updater), new Expression[2] {
            source.Expression,
            Expression.Quote(updater)
        }));

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <param name="source">源</param>
        /// <returns></returns>
        public static int Delete<T>(this IExecuteable<T> source)
        => source.Provider.Execute(Expression.Call(null, GetMethodInfo(Delete, source), new Expression[1] { source.Expression }));

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <param name="source">源</param>
        /// <param name="expression">条件表达式</param>
        /// <returns></returns>
        public static int Delete<T>(this IExecuteable<T> source, Expression<Func<T, bool>> expression)
        => source.Where(expression).Delete();

        /// <summary>
        /// 插入数据
        /// </summary>
        /// <typeparam name="T">实体</typeparam>
        /// <param name="source">源</param>
        /// <param name="selector">数据源</param>
        /// <returns></returns>
        public static int Insert<T>(this IExecuteable<T> source, IQueryable<T> selector)
            => source.Provider.Execute(Expression.Call(null, GetMethodInfo(Insert, source, selector), new Expression[2] {
                source.Expression,
                selector.Expression
            }));

    }
}

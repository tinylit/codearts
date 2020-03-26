using CodeArts.ORM;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq
{
    /// <summary>
    /// 查询器扩展
    /// </summary>
    public static class SelectExtentions
    {
        private static readonly ConcurrentDictionary<Type, IDbRepositoryProvider> ProviderCache = new ConcurrentDictionary<Type, IDbRepositoryProvider>();
        private static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 _, T2 _2) => f.Method;

        /// <summary>
        /// 指定表查询
        /// </summary>
        /// <typeparam name="TSource">资源类型</typeparam>
        /// <param name="source">查询器</param>
        /// <param name="table">表名称工厂</param>
        /// <returns></returns>
        public static IQueryable<TSource> From<TSource>(this IQueryable<TSource> source, Func<ITableRegions, string> table)
        => source.Provider.CreateQuery<TSource>(Expression.Call(null, GetMethodInfo(From, source, table), new Expression[2] {
            source.Expression,
            Expression.Constant(table ?? throw new ArgumentNullException(nameof(table)))
        }));

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。
        /// </summary>
        /// <typeparam name="TSource">资源类型</typeparam>
        /// <param name="source">查询器</param>
        /// <param name="commandTimeout">超时时间，单位：秒。<see cref="System.Data.IDbCommand.CommandTimeout"/></param>
        /// <returns></returns>
        public static IQueryable<TSource> TimeOut<TSource>(this IQueryable<TSource> source, int commandTimeout)
            => source.Provider.CreateQuery<TSource>(Expression.Call(null, GetMethodInfo(TimeOut, source, commandTimeout), new Expression[2] {
            source.Expression,
            Expression.Constant(commandTimeout)
        }));

        /// <summary>
        /// 查询第一个
        /// </summary>
        /// <typeparam name="TSource">源</typeparam>
        /// <typeparam name="TResult">结果</typeparam>
        /// <param name="source">源</param>
        /// <param name="selector">选择器</param>
        /// <returns></returns>
        public static TResult One<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        => source.Provider.Execute<TResult>(Expression.Call(null, GetMethodInfo(One, source, selector), new Expression[2] {
                source.Expression,
                Expression.Quote(selector ?? throw new ArgumentNullException(nameof(selector)))
            }));

        /// <summary>
        /// 查询最后一个
        /// </summary>
        /// <typeparam name="TSource">源</typeparam>
        /// <typeparam name="TResult">结果</typeparam>
        /// <param name="source">源</param>
        /// <param name="selector">选择器</param>
        /// <returns></returns>
        public static TResult LastOne<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector) where TResult : class
        => source.Provider.Execute<TResult>(Expression.Call(null, GetMethodInfo(LastOne, source, selector), new Expression[2] {
                source.Expression,
                Expression.Quote(selector ?? throw new ArgumentNullException(nameof(selector)))
            }));

        /// <summary>
        /// SQL
        /// </summary>
        /// <typeparam name="TSource">资源类型</typeparam>
        /// <param name="source">查询器</param>
        /// <returns></returns>
        public static string Sql<TSource>(this IQueryable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            IDbRepositoryProvider provider = ProviderCache.GetOrAdd(source.GetType(), type =>
            {
                if (!typeof(Repository<TSource>).IsAssignableFrom(type))
                {
                    throw new NotImplementedException("未实现数据仓储!");
                }

                var info = type.GetProperty("DbProvider", BindingFlags.Instance | BindingFlags.NonPublic);

                if (info is null)
                    throw new NullReferenceException("DbProvider");

                var value = info.GetValue(source, null);

                if (value is null)
                    throw new NullReferenceException("DbProvider");

                if (value is IDbRepositoryProvider dbRepositoryProvider)
                {
                    return dbRepositoryProvider;
                }

                throw new NullReferenceException("DbProvider");
            });

            using (var builder = provider.Create())
            {
                builder.Evaluate(source.Expression);

                return builder.ToSQL();
            }
        }
    }
}

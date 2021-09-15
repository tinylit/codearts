using CodeArts.Db;
using CodeArts.Db.Lts;
using System.Linq.Expressions;

namespace System.Linq
{
    /// <summary>
    /// 仓库扩展。
    /// </summary>
    public static class RepositoryExtentions
    {
        /// <summary>
        /// 指定表查询。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="table">表名称工厂。</param>
        /// <returns></returns>
        public static IQueryable<TSource> From<TSource>(this IRepository<TSource> source, Func<ITableInfo, string> table)
        => source.Provider.CreateQuery<TSource>(Expression.Call(null, QueryableMethods.From.MakeGenericMethod(typeof(TSource)), new Expression[2] {
            source.Expression,
            Expression.Constant(table ?? throw new ArgumentNullException(nameof(table)))
        }));

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="commandTimeout">超时时间，单位：秒。<see cref="Data.IDbCommand.CommandTimeout"/></param>
        /// <returns></returns>
        public static IQueryable<TSource> TimeOut<TSource>(this IQueryable<TSource> source, int commandTimeout)
        => source.Provider.CreateQuery<TSource>(Expression.Call(null, QueryableMethods.TimeOut.MakeGenericMethod(typeof(TSource)), new Expression[2] {
            source.Expression,
            Expression.Constant(commandTimeout)
        }));

        /// <summary>
        /// 未查询到数据的异常消息。仅支持以下方法（其它方法不生效）：
        /// <br/><see cref="Queryable.First{TSource}(IQueryable{TSource})"/>
        /// <br/><seealso cref="Queryable.First{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// <br/><seealso cref="Queryable.Last{TSource}(IQueryable{TSource})"/>
        /// <br/><seealso cref="Queryable.Last{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// <br/><seealso cref="Queryable.Single{TSource}(IQueryable{TSource})"/>
        /// <br/><seealso cref="Queryable.Single{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// <br/><seealso cref="Queryable.ElementAt{TSource}(IQueryable{TSource}, int)"/>
        /// <br/><seealso cref="Queryable.Max{TSource}(IQueryable{TSource})"/>
        /// <br/><seealso cref="Queryable.Max{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}})"/>
        /// <br/><seealso cref="Queryable.Min{TSource}(IQueryable{TSource})"/>
        /// <br/><seealso cref="Queryable.Min{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}})"/>
        /// <br/><seealso cref="Queryable.Average(IQueryable{decimal})"/>
        /// <br/><seealso cref="Queryable.Average(IQueryable{decimal?})"/>
        /// <br/><seealso cref="Queryable.Average(IQueryable{double})"/>
        /// <br/><seealso cref="Queryable.Average(IQueryable{double?})"/>
        /// <br/><seealso cref="Queryable.Average(IQueryable{float})"/>
        /// <br/><seealso cref="Queryable.Average(IQueryable{float?})"/>
        /// <br/><seealso cref="Queryable.Average(IQueryable{int})"/>
        /// <br/><seealso cref="Queryable.Average(IQueryable{long})"/>
        /// <br/><seealso cref="Queryable.Average(IQueryable{long?})"/>
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="errMsg">错误信息。</param>
        /// <returns></returns>
        public static IQueryable<TSource> NoResultError<TSource>(this IQueryable<TSource> source, string errMsg)
        => source.Provider.CreateQuery<TSource>(Expression.Call(null, QueryableMethods.NoResultError.MakeGenericMethod(typeof(TSource)), new Expression[2] {
            source.Expression,
            Expression.Constant(errMsg)
        }));

        /// <summary>
        /// SQL监视器。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="watchSql">监视器。</param>
        /// <returns></returns>
        public static IQueryable<TSource> WatchSql<TSource>(this IQueryable<TSource> source, Action<CommandSql> watchSql)
        => source.Provider.CreateQuery<TSource>(Expression.Call(null, QueryableMethods.WatchSql.MakeGenericMethod(typeof(TSource)), new Expression[2] {
                    source.Expression,
                    Expression.Constant(watchSql)
        }));
    }
}

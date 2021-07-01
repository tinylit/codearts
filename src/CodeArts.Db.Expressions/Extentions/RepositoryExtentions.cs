using CodeArts.Db;
using System.Linq.Expressions;

namespace System.Linq
{
    /// <summary>
    /// 仓库扩展。
    /// </summary>
    internal static class RepositoryExtentions
    {
        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="commandTimeout">超时时间，单位：秒。<see cref="Data.IDbCommand.CommandTimeout"/></param>
        /// <returns></returns>
        internal static IQueryable<TSource> TimeOut<TSource>(IQueryable<TSource> source, int commandTimeout)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (commandTimeout < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(commandTimeout));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 未查询到数据的异常消息。仅支持以下方法（其它方法不生效）：
        /// <br/><see cref="Queryable.First{TSource}(IQueryable{TSource})"/>
        /// <br/><seealso cref="Queryable.First{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// <br/><seealso cref="Queryable.Last{TSource}(IQueryable{TSource})"/>
        /// <br/><seealso cref="Queryable.Last{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// <br/><seealso cref="Queryable.Single{TSource}(IQueryable{TSource})"/>
        /// <br/><seealso cref="Queryable.Single{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// <br/><seealso cref="Queryable.ElementAt{TSource}(IQueryable{TSource}, int)"/>
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="errMsg">错误信息。</param>
        /// <returns></returns>
        internal static IQueryable<TSource> NoResultError<TSource>(IQueryable<TSource> source, string errMsg)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrEmpty(errMsg))
            {
                throw new ArgumentException($"“{nameof(errMsg)}”不能为 null 或空。", nameof(errMsg));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 指定表查询。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="table">表名称工厂。</param>
        /// <returns></returns>
        internal static IQueryable<TSource> From<TSource>(IQueryable<TSource> source, Func<ITableInfo, string> table)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (table is null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 更新。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="updateExp">更新表达式。</param>
        /// <returns></returns>
        internal static int Update<TSource>(IQueryable<TSource> source, Expression<Func<TSource, TSource>> updateExp)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (updateExp is null)
            {
                throw new ArgumentNullException(nameof(updateExp));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 删除。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <returns></returns>
        internal static int Delete<TSource>(IQueryable<TSource> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 删除。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="whereExp">删除条件。</param>
        /// <returns></returns>
        internal static int Delete<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> whereExp)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (whereExp is null)
            {
                throw new ArgumentNullException(nameof(whereExp));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 插入。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="querable">查询插入数据的表达式。</param>
        /// <returns></returns>
        internal static int Insert<TSource>(IQueryable<TSource> source, IQueryable<TSource> querable)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (querable is null)
            {
                throw new ArgumentNullException(nameof(querable));
            }

            throw new NotImplementedException();
        }
    }
}

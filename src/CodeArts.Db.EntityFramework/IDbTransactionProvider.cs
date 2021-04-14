using System;
#if NET_CORE
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 数据供应器。
    /// </summary>
    public interface IDbTransactionProvider
    {
        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <param name="dbContexts">上下文集合。</param>
        /// <returns></returns>
        IDbTransaction BeginTransaction(params DbContext[] dbContexts);

        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <param name="repositories">仓库集合。</param>
        /// <returns></returns>
        IDbTransaction BeginTransaction(params ILinqRepository[] repositories);
    }
}

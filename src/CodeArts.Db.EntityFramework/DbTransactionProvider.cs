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
    public class DbTransactionProvider : IDbTransactionProvider
    {
        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <param name="dbContexts">上下文集合。</param>
        /// <returns></returns>
        public IDbTransaction BeginTransaction(params DbContext[] dbContexts) => new DbTransaction(dbContexts);

        /// <summary>
        /// 使用事务。
        /// </summary>
        /// <param name="repositories">仓库集合。</param>
        /// <returns></returns>
        public IDbTransaction BeginTransaction(params ILinqRepository[] repositories) => new DbTransaction(repositories);
    }
}

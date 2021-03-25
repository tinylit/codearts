using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if NET_NORMAL || NET_CORE
using System.Threading;
using System.Threading.Tasks;
#endif

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据仓库。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
#if NET_NORMAL || NET_CORE
    public class DRepository<TEntity> : Repository<TEntity>, IDRepository<TEntity>, IRepository<TEntity>, IOrderedQueryable<TEntity>, IQueryable<TEntity>, IAsyncEnumerable<TEntity>, IEnumerable<TEntity>, IRepository, IAsyncQueryProvider, IQueryProvider, IOrderedQueryable, IQueryable, IEnumerable where TEntity : class, IEntiy
#else
    public class DRepository<TEntity> : Repository<TEntity>, IDRepository<TEntity>, IRepository<TEntity>, IOrderedQueryable<TEntity>, IQueryable<TEntity>, IEnumerable<TEntity>, IRepository, IQueryProvider, IOrderedQueryable, IQueryable, IEnumerable where TEntity : class, IEntiy
#endif
    {
        private static readonly Type TypeSelfEntity = typeof(TEntity);

        private static readonly MethodInfo FromMethod = QueryableMethods.From.MakeGenericMethod(TypeSelfEntity);
        private static readonly MethodInfo WhereMethod = QueryableMethods.Where.MakeGenericMethod(TypeSelfEntity);
        private static readonly MethodInfo TimeOutMethod = QueryableMethods.TimeOut.MakeGenericMethod(TypeSelfEntity);
        private static readonly MethodInfo UpdateMethod = QueryableMethods.Update.MakeGenericMethod(TypeSelfEntity);
        private static readonly MethodInfo DeleteMethod = QueryableMethods.Delete.MakeGenericMethod(TypeSelfEntity);
        private static readonly MethodInfo DeleteWithPredicateMethod = QueryableMethods.DeleteWithPredicate.MakeGenericMethod(TypeSelfEntity);
        private static readonly MethodInfo InsertMethod = QueryableMethods.Insert.MakeGenericMethod(TypeSelfEntity);

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DRepository() => DbContext = CreateDb(GetDbConfig());

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connectionConfig">链接配置。</param>
        public DRepository(IReadOnlyConnectionConfig connectionConfig) : base(connectionConfig) => DbContext = CreateDb(connectionConfig);

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="context">数据上下文。</param>
        public DRepository(IDbContext<TEntity> context) : base(context)
        {
            DbContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="context">链接配置。</param>
        /// <param name="expression">表达式。</param>
        private DRepository(IDbContext<TEntity> context, Expression expression) : base(context, expression)
        {
            DbContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// 上下文。
        /// </summary>
        protected IDbContext<TEntity> DbContext { get; }

        /// <summary>
        /// 创建上下文。
        /// </summary>
        /// <returns></returns>
        protected override IDbContext Create(IReadOnlyConnectionConfig connectionConfig) => CreateDb(connectionConfig);

        /// <summary>
        /// 创建上下文。
        /// </summary>
        /// <returns></returns>
        protected virtual IDbContext<TEntity> CreateDb(IReadOnlyConnectionConfig connectionConfig) => new DbContext<TEntity>(connectionConfig);

        /// <summary>
        /// 数据来源。
        /// </summary>
        /// <param name="table">表。</param>
        /// <returns></returns>
        public IDRepository<TEntity> From(Func<ITableInfo, string> table)
        {
            return new DRepository<TEntity>(DbContext, Expression.Call(null, FromMethod, new Expression[2] { Expression, Expression.Constant(table ?? throw new ArgumentNullException(nameof(table))) }));
        }

        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        public IDRepository<TEntity> Where(Expression<Func<TEntity, bool>> expression)
        {
            return new DRepository<TEntity>(DbContext, Expression.Call(null, WhereMethod, new Expression[2] { Expression, Expression.Quote(expression) }));
        }

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。
        /// </summary>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public IDRepository<TEntity> TimeOut(int commandTimeout)
        {
            return new DRepository<TEntity>(DbContext, Expression.Call(null, TimeOutMethod, new Expression[2] { Expression, Expression.Constant(commandTimeout) }));
        }

        /// <summary>
        /// 更新数据。
        /// </summary>
        /// <param name="updateExp">更新的字段和值。</param>
        /// <returns></returns>
        public int Update(Expression<Func<TEntity, TEntity>> updateExp)
        {
            return DbContext.Execute(Expression.Call(null, UpdateMethod, new Expression[2] { Expression, Expression.Quote(updateExp) }));
        }

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <returns></returns>
        public int Delete()
        {
            return DbContext.Execute(Expression.Call(null, DeleteMethod, new Expression[1] { Expression }));
        }

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="whereExp">条件表达式。</param>
        /// <returns></returns>
        public int Delete(Expression<Func<TEntity, bool>> whereExp)
        {
            return DbContext.Execute(Expression.Call(null, DeleteWithPredicateMethod, new Expression[2] { Expression, Expression.Quote(whereExp) }));
        }

        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="querable">需要插入的数据。</param>
        /// <returns></returns>
        public int Insert(IQueryable<TEntity> querable)
        {
            return DbContext.Execute(Expression.Call(null, InsertMethod, new Expression[2] { Expression, querable.Expression }));
        }

#if NET_NORMAL || NET_CORE
        /// <summary>
        /// 更新数据。
        /// </summary>
        /// <param name="updateExp">更新的字段和值。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> UpdateAsync(Expression<Func<TEntity, TEntity>> updateExp, CancellationToken cancellationToken = default)
        {
            return DbContext.ExecuteAsync(Expression.Call(null, UpdateMethod, new Expression[2] { Expression, Expression.Quote(updateExp) }), cancellationToken);
        }

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> DeleteAsync(CancellationToken cancellationToken = default)
        {
            return DbContext.ExecuteAsync(Expression.Call(null, DeleteMethod, new Expression[1] { Expression }), cancellationToken);
        }

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="whereExp">条件表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExp, CancellationToken cancellationToken = default)
        {
            return DbContext.ExecuteAsync(Expression.Call(null, DeleteWithPredicateMethod, new Expression[2] { Expression, Expression.Quote(whereExp) }), cancellationToken);
        }

        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="querable">需要插入的数据。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> InsertAsync(IQueryable<TEntity> querable, CancellationToken cancellationToken = default)
        {
            return DbContext.ExecuteAsync(Expression.Call(null, InsertMethod, new Expression[2] { Expression, querable.Expression }), cancellationToken);
        }
#endif
    }
}

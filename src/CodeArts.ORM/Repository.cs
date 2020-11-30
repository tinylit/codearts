using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.ORM
{
    /// <summary>
    /// 数据仓库。
    /// </summary>
    public abstract class Repository
    {
        private static readonly ConcurrentDictionary<Type, DbConfigAttribute> DbConfigCache = new ConcurrentDictionary<Type, DbConfigAttribute>();
        private readonly IReadOnlyConnectionConfig connectionConfig;

        /// <summary>
        /// 数据上下文。
        /// </summary>
        protected IContext Context { get; }

        /// <summary>
        /// 获取数据库配置。
        /// </summary>
        /// <returns></returns>
        protected virtual IReadOnlyConnectionConfig GetDbConfig()
        {
            if (connectionConfig is null)
            {
                var attr = DbConfigCache.GetOrAdd(GetType(), type =>
                {
                    return (DbConfigAttribute)Attribute.GetCustomAttribute(type, typeof(DbConfigAttribute));
                });

                if (attr is null)
                {
                    return default;
                }

                return attr.GetConfig();
            }

            return connectionConfig;
        }

        /// <summary>
        /// 链接。
        /// </summary>
        protected Repository() => Context = Create(GetDbConfig());

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connectionConfig">链接配置。</param>
        public Repository(IReadOnlyConnectionConfig connectionConfig)
        {
            if (connectionConfig is null)
            {
                throw new ArgumentNullException(nameof(connectionConfig));
            }

            this.connectionConfig = connectionConfig;

            Context = Create(connectionConfig);
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="context">数据库上下文。</param>
        public Repository(IContext context) => Context = context ?? throw new ArgumentNullException(nameof(context));

        /// <summary>
        /// 创建上下文。
        /// </summary>
        /// <param name="connectionConfig">链接配置。</param>
        /// <returns></returns>
        protected virtual IContext Create(IReadOnlyConnectionConfig connectionConfig) => new Context(connectionConfig);
    }

    /// <summary>
    /// 数据仓储。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
#if NET_NORMAL
    public class Repository<T> : Repository, IRepository<T>, IOrderedQueryable<T>, IQueryable<T>, IAsyncEnumerable<T>, IEnumerable<T>, IOrderedQueryable, IQueryable, IAsyncQueryProvider, IQueryProvider, IEnumerable
#else
    public class Repository<T> : Repository, IRepository<T>, IQueryable<T>, IEnumerable<T>, IQueryable, IQueryProvider, IEnumerable
#endif
    {
        private static readonly ConcurrentDictionary<Type, DbConfigAttribute> DbConfigCache = new ConcurrentDictionary<Type, DbConfigAttribute>();

        private readonly bool isEmpty = false;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public Repository() => isEmpty = true;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="connectionConfig">链接配置。</param>
        public Repository(IReadOnlyConnectionConfig connectionConfig) : base(connectionConfig) => isEmpty = true;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="context">链接配置。</param>
        public Repository(IContext context) : base(context) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="context">链接配置。</param>
        /// <param name="expression">表达式。</param>
        protected Repository(IContext context, Expression expression) : base(context)
        {
            this.expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        /// <summary>
        /// 当前元素类型。
        /// </summary>
        public Type ElementType => typeof(T);

        private readonly Expression expression = null;
        private Expression _ContextExpression = null;

        /// <summary>
        /// 查询供应器。
        /// </summary>
        public IQueryProvider Provider => this;

        /// <summary>
        /// 迭代器。
        /// </summary>
        protected IEnumerable<T> Enumerable { private set; get; }

        /// <summary>
        /// 获取数据库配置。
        /// </summary>
        /// <returns></returns>
        protected override IReadOnlyConnectionConfig GetDbConfig()
        {
            var config = base.GetDbConfig();

            if (config is null)
            {
                var attr = DbConfigCache.GetOrAdd(typeof(T), type =>
                {
                    return (DbConfigAttribute)Attribute.GetCustomAttribute(type, typeof(DbConfigAttribute));
                });

                if (attr is null)
                {
                    return config;
                }

                return attr.GetConfig();
            }

            return config;
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            Type type = expression.Type.FindGenericType(typeof(IQueryable<>));

            if (type is null)
            {
                throw new ArgumentException("无效表达式!", nameof(expression));
            }

            Type type2 = typeof(Repository<>).MakeGenericType(type.GetGenericArguments());

            return (IQueryable)Activator.CreateInstance(type2, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[2]
            {
                Context,
                expression
            }, null);
        }

        /// <summary>
        /// 创建IQueryable。
        /// </summary>
        /// <typeparam name="TElement">泛型类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => new Repository<TElement>(Context, expression);

        /// <summary>
        /// 执行表达式。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        object IQueryProvider.Execute(Expression expression) => Context.Read<T>(expression ?? throw new ArgumentNullException(nameof(expression)));

        /// <summary>
        /// 执行结果。
        /// </summary>
        /// <typeparam name="TResult">结果。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        private TResult Execute<TResult>(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (!typeof(TResult).IsAssignableFrom(expression.Type))
            {
                throw new NotImplementedException(nameof(expression));
            }

            if (typeof(IEnumerable<T>).IsAssignableFrom(typeof(TResult)))
            {
                throw new NotSupportedException(nameof(expression));
            }

            return Context.Read<TResult>(expression);
        }

        /// <summary>
        /// 执行表达式。
        /// </summary>
        /// <typeparam name="TResult">返回结果类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        TResult IQueryProvider.Execute<TResult>(Expression expression) => Execute<TResult>(expression);

        /// <summary>
        /// 表达式。
        /// </summary>
        public Expression Expression => expression ?? _ContextExpression ?? (_ContextExpression = Expression.Constant(this));

        private IEnumerator<T> GetEnumerator()
        {
            if (isEmpty || Enumerable is null)
            {
                Enumerable = Context.Query<T>(Expression);
            }

            return Enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

#if NET_NORMAL
        /// <summary>
        /// 异步消息。
        /// </summary>
        /// <typeparam name="TResult">结果。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<TResult> IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (!typeof(TResult).IsAssignableFrom(expression.Type))
            {
                throw new NotImplementedException(nameof(expression));
            }

            if (typeof(IEnumerable<T>).IsAssignableFrom(typeof(TResult)))
            {
                throw new NotSupportedException(nameof(expression));
            }

            return Context.ReadAsync<TResult>(expression);
        }

        private IAsyncEnumerable<T> AsyncEnumerable;

        /// <summary>
        /// 获取异步迭代器。
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator()
        {
            if (isEmpty || Enumerable is null)
            {
                AsyncEnumerable = Context.QueryAsync<T>(Expression);
            }

            return AsyncEnumerable.GetAsyncEnumerator();
        }
#endif
    }
}

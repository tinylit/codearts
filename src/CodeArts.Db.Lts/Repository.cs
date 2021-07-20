using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 数据仓储。
    /// </summary>
    /// <typeparam name="T">实体类型。</typeparam>
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
    public class Repository<T> : IRepository<T>, IOrderedQueryable<T>, IQueryable<T>, IAsyncEnumerable<T>, IEnumerable<T>, IRepository, IOrderedQueryable, IQueryable, IAsyncQueryProvider, IQueryProvider, IEnumerable, IDisposable
#else
    public class Repository<T> : IRepository<T>, IQueryable<T>, IEnumerable<T>, IRepository, IQueryable, IQueryProvider, IEnumerable
#endif
    {
        private readonly IReadOnlyConnectionConfig connectionConfig;
        private static readonly ConcurrentDictionary<Type, DbConfigAttribute> DbConfigCache = new ConcurrentDictionary<Type, DbConfigAttribute>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        public Repository() => Database = Create(GetDbConfig());

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

            Database = Create(this.connectionConfig = connectionConfig);
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="database">数据库。</param>
        public Repository(IDatabase database) => Database = database ?? throw new ArgumentNullException(nameof(database));

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="database">链接配置。</param>
        /// <param name="expression">表达式。</param>
        protected Repository(IDatabase database, Expression expression) : this(database)
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
        /// 数据库。
        /// </summary>
        protected IDatabase Database { get; }

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
                    return (DbConfigAttribute)(Attribute.GetCustomAttribute(type, typeof(DbReadConfigAttribute)) ?? Attribute.GetCustomAttribute(type, typeof(DbConfigAttribute)));
                }) ?? DbConfigCache.GetOrAdd(ElementType, type =>
                {
                    return (DbConfigAttribute)(Attribute.GetCustomAttribute(type, typeof(DbReadConfigAttribute)) ?? Attribute.GetCustomAttribute(type, typeof(DbConfigAttribute)));
                });

                return attr.GetConfig();
            }

            return connectionConfig;
        }

        /// <summary>
        /// 创建数据库。
        /// </summary>
        /// <param name="connectionConfig">链接配置。</param>
        /// <returns></returns>
        protected virtual IDatabase Create(IReadOnlyConnectionConfig connectionConfig) => DatabaseFactory.Create(connectionConfig);

        /// <summary>
        /// 查找指定类型。
        /// </summary>
        /// <returns></returns>
        private static Type FindGenericType(Type type, Type definition)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
                {
                    return type;
                }

                if (definition.IsInterface)
                {
                    Type[] interfaces = type.GetInterfaces();

                    foreach (Type type2 in interfaces)
                    {
                        Type type3 = FindGenericType(type2, definition);

                        if (type3 is null) continue;

                        return type3;
                    }
                }

                type = type.BaseType;
            }
            return null;
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            Type type = FindGenericType(expression.Type, typeof(IQueryable<>));

            if (type is null)
            {
                throw new ArgumentException("无效表达式!", nameof(expression));
            }

            Type type2 = typeof(Repository<>).MakeGenericType(type.GetGenericArguments());

            return (IQueryable)Activator.CreateInstance(type2, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[2]
            {
                Database,
                expression
            }, null);
        }

        /// <summary>
        /// 创建IQueryable。
        /// </summary>
        /// <typeparam name="TElement">泛型类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => new Repository<TElement>(Database, expression);

        /// <summary>
        /// 执行表达式。
        /// </summary>
        /// <param name="expression">表达式。</param>
        /// <returns></returns>
        object IQueryProvider.Execute(Expression expression) => Database.Single<T>(expression ?? throw new ArgumentNullException(nameof(expression)));

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

            return Database.Single<TResult>(expression);
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
        public Expression Expression
#if NETSTANDARD2_1_OR_GREATER
            => expression ?? (_ContextExpression ??= Expression.Constant(this));
#else
            => expression ?? _ContextExpression ?? (_ContextExpression = Expression.Constant(this));
#endif

        private IEnumerator<T> GetEnumerator()
        {
            if (Enumerable is null)
            {
                Enumerable = Database.Query<T>(Expression);
            }

            return Enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER
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

            return Database.SingleAsync<TResult>(expression);
        }

        private IAsyncEnumerable<T> AsyncEnumerable;

        /// <summary>
        /// 获取异步迭代器。
        /// </summary>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            if (AsyncEnumerable is null)
            {
                AsyncEnumerable = Database.QueryAsync<T>(Expression);
            }

            return AsyncEnumerable.GetAsyncEnumerator(cancellationToken);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Database.Dispose();

                    GC.SuppressFinalize(this);
                }

                disposedValue = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
        }
        #endregion
#endif
    }
}

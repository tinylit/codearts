using SkyBuilding.Config;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 数据仓库
    /// </summary>
    public abstract class Repository
    {
        private readonly static ConcurrentDictionary<Type, DbConfigAttribute> mapperCache = new ConcurrentDictionary<Type, DbConfigAttribute>();

        private IDbConnectionAdapter _DbProvider = null;
        /// <summary>
        /// 链接配置
        /// </summary>
        protected IReadOnlyConnectionConfig ConnectionConfig { private set; get; }
        /// <summary>
        /// 数据库链接
        /// </summary>
        protected IDbConnection Connection => TransactionScopeConnections.GetConnection(ConnectionConfig.ConnectionString, DbAdapter) ?? ThreadScopeConnections.Instance.GetConnection(ConnectionConfig.ConnectionString, DbAdapter);
        /// <summary>
        /// 数据库适配器
        /// </summary>
        protected IDbConnectionAdapter DbAdapter => _DbProvider ?? (_DbProvider = DbConnectionManager.Create(ConnectionConfig.ProviderName));
        /// <summary>
        /// SQL矫正设置
        /// </summary>
        public ISQLCorrectSettings Settings => DbAdapter.Settings;

        /// <summary>
        /// 获取仓储数据库配置属性
        /// </summary>
        /// <returns></returns>
        private DbConfigAttribute GetAttribute() => mapperCache.GetOrAdd(GetType(), type =>
        {
            var attr = (DbConfigAttribute)Attribute.GetCustomAttribute(type, typeof(DbConfigAttribute));

            if (attr == null)
            {
                attr = (DbConfigAttribute)Attribute.GetCustomAttribute(type.BaseType, typeof(DbConfigAttribute));
            }

            return attr ?? throw new NotImplementedException("仓库类未指定数据库链接属性!");
        });

        /// <summary>
        /// 获取数据库配置
        /// </summary>
        /// <returns></returns>
        protected virtual ConnectionConfig GetDbConfig()
        {
            var attr = GetAttribute();

            ConfigHelper.Instance.OnConfigChanged += _ =>
            {
                ConnectionConfig = attr.GetConfig();
            };

            return attr.GetConfig();
        }

        /// <summary>
        /// 链接
        /// </summary>
        /// <param name="connectionConfig">链接配置</param>
        protected Repository() => ConnectionConfig = GetDbConfig() ?? throw new NoNullAllowedException("未找到数据链接配置信息!");

        /// <summary>
        /// 链接
        /// </summary>
        /// <param name="connectionConfig">链接配置</param>
        public Repository(IReadOnlyConnectionConfig connectionConfig) => ConnectionConfig = connectionConfig ?? throw new ArgumentNullException(nameof(connectionConfig));

        /// <summary> 执行数据库事务 </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="factory">事件工厂</param>
        /// <param name="level">指定连接的事务锁定行为。</param>
        /// <returns></returns>
        protected TResult Transaction<TResult>(Func<IDbConnection, IDbTransaction, TResult> factory, IsolationLevel? level = null)
        {
            using (IDbConnection dbConnection = ThreadScopeConnections.Instance.GetConnection(ConnectionConfig.ConnectionString, DbAdapter, false))
            {
                try
                {
                    dbConnection.Open();
                    using (var transaction = level.HasValue ? dbConnection.BeginTransaction(level.Value) : dbConnection.BeginTransaction())
                    {
                        try
                        {
                            var result = factory.Invoke(dbConnection, transaction);
                            transaction.Commit();
                            return result;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                finally
                {
                    dbConnection.Close();
                }
            }
        }
    }

    /// <summary>
    /// 数据仓储
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Repository<T> : Repository, IRepository<T>, IOrderedQueryable<T>, IQueryable<T>, IEnumerable<T>, IOrderedQueryable, IQueryable, IEnumerable, IQueryProvider
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public Repository() : base()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionConfig">数据库链接</param>
        /// <param name="expression">表达式</param>
        private Repository(IReadOnlyConnectionConfig connectionConfig, Expression expression) : base(connectionConfig) => _Expression = expression ?? throw new ArgumentNullException(nameof(expression));

        /// <summary>
        /// 当前元素类型
        /// </summary>
        public Type ElementType => typeof(T);

        private readonly Expression _Expression = null;

        private Expression _ContextExpression = null;
        /// <summary>
        /// 查询供应器
        /// </summary>
        public IQueryProvider Provider => this;
        /// <summary>
        /// 迭代器
        /// </summary>
        protected IEnumerable<T> Enumerable { private set; get; }

        private IDbRepositoryProvider _DbProvider = null;
        /// <summary>
        /// 仓库供应器
        /// </summary>
        protected virtual IDbRepositoryProvider DbProvider => _DbProvider ?? (_DbProvider = DbConnectionManager.Create(DbAdapter));
        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            Type type = expression.Type.FindGenericType(typeof(IRepository<>));

            if (type is null)
            {
                throw new ArgumentException("无效表达式!", nameof(expression));
            }

            Type type2 = typeof(Repository<>).MakeGenericType(type.GetGenericArguments().First());

            return (IQueryable)Activator.CreateInstance(type2, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[2]
            {
                ConnectionConfig,
                expression
            }, null);
        }
        /// <summary>
        /// 创建IQueryable
        /// </summary>
        /// <typeparam name="TElement">泛型类型</typeparam>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => new Repository<TElement>(ConnectionConfig, expression);

        /// <summary>
        /// 执行表达式
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        object IQueryProvider.Execute(Expression expression) => Execute<T>(expression);

        /// <summary>
        /// 执行结果
        /// </summary>
        /// <typeparam name="TResult">结果</typeparam>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        private TResult Execute<TResult>(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (!typeof(TResult).IsAssignableFrom(expression.Type))
            {
                throw new NotImplementedException(nameof(expression));
            }

            return DbProvider.Evaluate<T, TResult>(Connection, expression);
        }

        /// <summary>
        /// 执行表达式
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        /// <param name="expression">表达式</param>
        /// <returns></returns>
        TResult IQueryProvider.Execute<TResult>(Expression expression) => Execute<TResult>(expression);

        /// <summary>
        /// 表达式
        /// </summary>
        public Expression Expression => _Expression ?? _ContextExpression ?? (_ContextExpression = Expression.Constant(this));

        private IEnumerator<T> GetEnumerator()
        {
            if (_Expression == null)
            {
                return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
            }

            if (Enumerable == null)
            {
                Enumerable = Provider.Execute<IEnumerable<T>>(Expression);
            }
            return Enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    }
}

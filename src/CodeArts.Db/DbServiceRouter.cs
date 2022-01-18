using CodeArts.Runtime;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db
{
    /// <summary>
    /// 路由执行器。
    /// </summary>
    public static class DbServiceRouter<TEntity> where TEntity : class, IEntiy
    {
        /// <summary>
        /// 单例。
        /// </summary>
        public static IDbRouter<TEntity> Instance { get; }

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static DbServiceRouter() => Instance = RuntimeServPools.Singleton<IDbRouter<TEntity>, DefaultDbRouter>();

        private sealed class DefaultDbRouter : IDbRouter<TEntity>
        {
            public string[] Except<TColumn>(Expression<Func<TEntity, TColumn>> lamda) => Limit(lamda);

            public string[] Limit<TColumn>(Expression<Func<TEntity, TColumn>> lamda)
            {
                if (lamda is null)
                {
                    throw new ArgumentNullException(nameof(lamda));
                }

                var parameter = lamda.Parameters[0];

                var body = lamda.Body;

                switch (body.NodeType)
                {
                    case ExpressionType.Constant when body is ConstantExpression constant:
                        switch (constant.Value)
                        {
                            case string text:
                                return text.Split(',', ' ');
                            case string[] arr:
                                return arr;
                            default:
                                throw new NotImplementedException();
                        }
                    case ExpressionType.MemberAccess when body is MemberExpression member:
                        return new string[] { member.Member.Name };
                    case ExpressionType.MemberInit when body is MemberInitExpression memberInit:
                        return memberInit.Bindings.Select(x => x.Member.Name).ToArray();
                    case ExpressionType.New when body is NewExpression newExpression:
                        return newExpression.Members.Select(x => x.Name).ToArray();
                    case ExpressionType.Parameter:
                        var storeItem = TypeItem.Get(parameter.Type);
                        return storeItem.PropertyStores
                            .Where(x => x.CanRead && x.CanWrite)
                            .Select(x => x.Name)
                            .ToArray();
                    default:
                        throw new NotSupportedException($"不支持表达式({lamda})!");
                }
            }

            public string[] Where<TColumn>(Expression<Func<TEntity, TColumn>> lamda)
            {
                if (lamda is null)
                {
                    throw new ArgumentNullException(nameof(lamda));
                }

                var parameter = lamda.Parameters[0];

                var body = lamda.Body;

                switch (body.NodeType)
                {
                    case ExpressionType.Constant when body is ConstantExpression constant:
                        switch (constant.Value)
                        {
                            case string text:
                                return text.Split(',', ' ');
                            case string[] arr:
                                return arr;
                            default:
                                throw new NotImplementedException();
                        }
                    case ExpressionType.MemberAccess when body is MemberExpression member:
                        return new string[] { member.Member.Name };
                    case ExpressionType.MemberInit when body is MemberInitExpression memberInit:
                        return memberInit.Bindings.Select(x => x.Member.Name).ToArray();
                    case ExpressionType.New when body is NewExpression newExpression:
                        return newExpression.Members.Select(x => x.Name).ToArray();
                    case ExpressionType.Parameter:
                        var storeItem = TypeItem.Get(parameter.Type);
                        return storeItem.PropertyStores
                            .Where(x => x.CanRead)
                            .Select(x => x.Name)
                            .ToArray();
                    default:
                        throw new NotSupportedException($"不支持表达式({lamda})!");
                }
            }
        }
    }
}
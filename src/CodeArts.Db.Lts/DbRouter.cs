using CodeArts.Runtime;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 路由执行器。
    /// </summary>
    public sealed class DbRouter<TEntity> : IDbRouter<TEntity> where TEntity : class, IEntiy
    {
        private static readonly ConcurrentDictionary<Type, string[]> LimitCahce = new ConcurrentDictionary<Type, string[]>();
        private static readonly ConcurrentDictionary<Type, string[]> ExceptCahce = new ConcurrentDictionary<Type, string[]>();

        private static readonly ConcurrentDictionary<Type, Func<TEntity, string[]>> LambdaCahce = new ConcurrentDictionary<Type, Func<TEntity, string[]>>();

        /// <summary>
        /// 单例。
        /// </summary>
        public static IDbRouter<TEntity> Instance { get; }

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static DbRouter() => Instance = RuntimeServPools.Singleton<IDbRouter<TEntity>, DbRouter<TEntity>>();

        private static Func<TEntity, string[]> Conditional(ParameterExpression parameter, Expression test, MemberExpression ifTrue, MemberExpression ifFalse)
        {
            var bodyExp = Expression.Condition(test, Expression.Constant(ifTrue.Member.Name), Expression.Constant(ifFalse.Member.Name));

            var lamdaExp = Expression.Lambda<Func<TEntity, string>>(bodyExp, parameter);

            var invoke = lamdaExp.Compile();

            return source => new string[] { invoke.Invoke(source) };
        }

        string[] IDbRouter<TEntity>.Except<TColumn>(Expression<Func<TEntity, TColumn>> lamda) => Limit(lamda);

        string[] IDbRouter<TEntity>.Limit<TColumn>(Expression<Func<TEntity, TColumn>> lamda)
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
                    var storeItem = TypeStoreItem.Get(parameter.Type);
                    return storeItem.PropertyStores
                        .Where(x => x.CanRead && x.CanWrite)
                        .Select(x => x.Name)
                        .ToArray();
                default:
                    throw new NotImplementedException();
            }
        }

        private static Func<TEntity, string[]> Make(string[] columns) => _ => columns;

        Func<TEntity, string[]> IDbRouter<TEntity>.Where<TColumn>(Expression<Func<TEntity, TColumn>> lamda)
        {
            if (lamda is null)
            {
                throw new ArgumentNullException(nameof(lamda));
            }

            var parameter = lamda.Parameters[0];

            var body = lamda.Body;

            switch (body.NodeType)
            {
                case ExpressionType.Coalesce when body is BinaryExpression binary && binary.Left is MemberExpression left && binary.Right is MemberExpression right:
                    if (left.Type.IsValueType)
                    {
                        return Conditional(parameter, Expression.Parameter(left.Type, "HasValue"), left, right);
                    }
                    return Conditional(parameter, Expression.NotEqual(left, Expression.Default(binary.Type)), left, right);
                case ExpressionType.Conditional when body is ConditionalExpression conditional && conditional.IfTrue is MemberExpression left && conditional.IfFalse is MemberExpression right:
                    return Conditional(parameter, conditional.Test, left, right);
                case ExpressionType.NewArrayInit when body.Type == typeof(string[]):
                case ExpressionType.Call when body is MethodCallExpression methodCall && methodCall.Method.ReturnType == typeof(string[]):
                    return lamda.Compile() as Func<TEntity, string[]> ?? throw new NotImplementedException();
                case ExpressionType.Constant when body is ConstantExpression constant:
                    switch (constant.Value)
                    {
                        case string text:
                            return Make(text.Split(',', ' '));
                        case string[] arr:
                            return Make(arr);
                        default:
                            throw new NotImplementedException();
                    }
                case ExpressionType.MemberAccess when body is MemberExpression member:
                    return Make(new string[] { member.Member.Name });
                case ExpressionType.MemberInit when body is MemberInitExpression memberInit:
                    return Make(memberInit.Bindings.Select(x => x.Member.Name).ToArray());
                case ExpressionType.New when body is NewExpression newExpression:
                    return Make(newExpression.Members.Select(x => x.Name).ToArray());
                case ExpressionType.Parameter:
                    var storeItem = TypeStoreItem.Get(parameter.Type);
                    return Make(storeItem.PropertyStores
                        .Where(x => x.CanRead)
                        .Select(x => x.Name)
                        .ToArray());
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 排除字段。
        /// </summary>
        /// <typeparam name="TColumn">字段。</typeparam>
        /// <param name="lamda">表达式。</param>
        /// <returns></returns>
        public string[] Except<TColumn>(Expression<Func<TEntity, TColumn>> lamda) => ExceptCahce.GetOrAdd(typeof(TColumn), _ => Instance.Except(lamda));

        /// <summary>
        /// 限制字段。
        /// </summary>
        /// <typeparam name="TColumn">字段。</typeparam>
        /// <param name="lamda">表达式。</param>
        /// <returns></returns>
        public string[] Limit<TColumn>(Expression<Func<TEntity, TColumn>> lamda) => LimitCahce.GetOrAdd(typeof(TColumn), _ => Instance.Limit(lamda));

        /// <summary>
        /// 条件。
        /// </summary>
        /// <typeparam name="TColumn">字段。</typeparam>
        /// <param name="lamda">表达式。</param>
        /// <returns></returns>
        public Func<TEntity, string[]> Where<TColumn>(Expression<Func<TEntity, TColumn>> lamda) => LambdaCahce.GetOrAdd(typeof(TColumn), _ => Instance.Where(lamda));
    }
}

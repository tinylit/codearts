using CodeArts.ORM.Exceptions;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace CodeArts.ORM
{
    /// <summary>
    /// 路由执行器。
    /// </summary>
    public sealed class RouteProvider<T> : IRouteProvider<T>
    {
        /// <summary>
        /// 单例。
        /// </summary>
        public static IRouteProvider<T> Instance { private set; get; }

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static RouteProvider() => Instance = RuntimeServManager.Singleton<IRouteProvider<T>, RouteProvider<T>>(instance => Instance = instance);

        private static Func<T, string[]> Conditional(ParameterExpression parameter, Expression test, MemberExpression ifTrue, MemberExpression ifFalse)
        {
            var bodyExp = Expression.Condition(test, Expression.Constant(ifTrue.Member.Name), Expression.Constant(ifFalse.Member.Name));

            var lamdaExp = Expression.Lambda<Func<T, string>>(bodyExp, parameter);

            var invoke = lamdaExp.Compile();

            return source => new string[] { invoke.Invoke(source) };
        }

        string[] IRouteProvider<T>.Except<TColumn>(Expression<Func<T, TColumn>> lamda) => Limit(lamda);

        string[] IRouteProvider<T>.Limit<TColumn>(Expression<Func<T, TColumn>> lamda)
        {
            if (lamda.Parameters.Count > 1)
                throw new DSyntaxErrorException();

            var parameter = lamda.Parameters.First();

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
                    var storeItem = RuntimeTypeCache.Instance.GetCache(parameter.Type);
                    return storeItem.PropertyStores
                        .Where(x => x.CanRead && x.CanWrite)
                        .Select(x => x.Name)
                        .ToArray();
            }
            throw new NotImplementedException();
        }

        Func<T, string[]> IRouteProvider<T>.Where<TColumn>(Expression<Func<T, TColumn>> lamda)
        {
            if (lamda.Parameters.Count > 1)
                throw new DSyntaxErrorException();

            var parameter = lamda.Parameters.First();

            var body = lamda.Body;

            switch (body.NodeType)
            {
                case ExpressionType.Coalesce when body is BinaryExpression binary && binary.Left is MemberExpression left && binary.Right is MemberExpression right:
                    return Conditional(parameter, Expression.NotEqual(left, Expression.Default(binary.Type)), left, right);
                case ExpressionType.Conditional when body is ConditionalExpression conditional && conditional.IfTrue is MemberExpression left && conditional.IfFalse is MemberExpression right:
                    return Conditional(parameter, conditional.Test, left, right);
                case ExpressionType.NewArrayInit when body.Type == typeof(string[]):
                case ExpressionType.Call when body is MethodCallExpression methodCall && methodCall.Method.ReturnType == typeof(string[]):
                    return lamda.Compile() as Func<T, string[]> ?? throw new NotImplementedException();
                default:
                    return source => Limit(lamda);
            }
        }

        /// <summary>
        /// 排除字段。
        /// </summary>
        /// <typeparam name="TColumn">字段</typeparam>
        /// <param name="lamda">表达式</param>
        /// <returns></returns>
        public string[] Except<TColumn>(Expression<Func<T, TColumn>> lamda) => Instance.Except(lamda);

        /// <summary>
        /// 限制字段。
        /// </summary>
        /// <typeparam name="TColumn">字段</typeparam>
        /// <param name="lamda">表达式</param>
        /// <returns></returns>
        public string[] Limit<TColumn>(Expression<Func<T, TColumn>> lamda) => Instance.Limit(lamda);

        /// <summary>
        /// 条件。
        /// </summary>
        /// <typeparam name="TColumn">字段</typeparam>
        /// <param name="lamda">表达式</param>
        /// <returns></returns>
        public Func<T, string[]> Where<TColumn>(Expression<Func<T, TColumn>> lamda) => Instance.Where(lamda);
    }
}

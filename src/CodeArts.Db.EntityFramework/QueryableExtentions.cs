using CodeArts.Runtime;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;
using static System.Linq.Expressions.Expression;
using CodeArts;

namespace System.Linq
{
    /// <summary>
    /// 扩展。
    /// </summary>
    public static class QueryableExtentions
    {
        private static readonly ConcurrentDictionary<long, Expression> Expressions = new ConcurrentDictionary<long, Expression>();

        private static readonly MethodInfo SelectMethod = GetSelectMethod();

        private static bool IsExpressionOfFunc(Type type) => type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(Expression<>)
            && type.GetGenericArguments()[0].IsGenericType
            && type.GetGenericArguments()[0].GetGenericArguments().Length == 2;
        private static MethodInfo GetSelectMethod()
        {
            return typeof(Queryable).GetMethods().First(x => x.Name == nameof(Queryable.Select) && x.GetParameters().Length == 2 && IsExpressionOfFunc(x.GetParameters()[1].ParameterType));
        }

        /// <summary>
        /// 映射查询（生成“<typeparamref name="TResult"/>”类型的查询表达式）。
        /// 注：名称相同（支持<see cref="NamingAttribute"/>比较），且可读写的属性映射。如：x => new YueClass { Id = x.Id, Name = x.Name }。
        /// </summary>
        /// <typeparam name="TResult">查询结果。</typeparam>
        /// <param name="source">数据源。</param>
        /// <returns></returns>
        public static IQueryable<TResult> Map<TResult>(this IQueryable source) where TResult : class, new()
        {
            if (source is IQueryable<TResult> queryable)
            {
                return queryable;
            }

            var conversionType = typeof(TResult);
            var elementType = source.ElementType;

            return source.Provider.CreateQuery<TResult>(Expressions.GetOrAdd(((long)elementType.MetadataToken << 31) + conversionType.MetadataToken, _ =>
            {
                var typeItem = TypeItem.Get(conversionType);
                var typeItem2 = TypeItem.Get(elementType);

                var parameterExp = Parameter(elementType, "x");

                var list = new List<MemberBinding>();

                typeItem.PropertyStores
                    .Where(x => x.CanWrite && x.CanRead)
                    .ForEach(x =>
                    {
                        var propItem = typeItem2.PropertyStores
                          .FirstOrDefault(y => y.Name == x.Name && y.MemberType == x.MemberType && y.CanRead) ?? typeItem2
                          .PropertyStores.FirstOrDefault(y => y.MemberType == x.MemberType && y.CanRead && y.Naming == x.Naming) ?? typeItem2
                          .PropertyStores.FirstOrDefault(y => y.MemberType == x.MemberType && y.CanRead && y.Name == x.Naming) ?? typeItem2
                          .PropertyStores.FirstOrDefault(y => y.MemberType == x.MemberType && y.CanRead && y.Naming == x.Name);

                        if (propItem is null)
                        {
                            return;
                        }

                        list.Add(Bind(x.Member, Property(parameterExp, propItem.Member)));
                    });

                var memberInit = MemberInit(New(typeItem.Type), list);

                var lambdaEx = Lambda(memberInit, parameterExp);

                return Call(SelectMethod.MakeGenericMethod(elementType, conversionType), new Expression[2] {
                        source.Expression,
                        lambdaEx
                    });
            }));
        }
    }
}

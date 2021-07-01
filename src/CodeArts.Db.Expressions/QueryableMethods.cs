using CodeArts.Db;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq
{
    /// <summary>
    ///     A class that provides reflection metadata for translatable LINQ methods.
    /// </summary>
    public static class QueryableMethods
    {
        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.AsQueryable" />
        /// </summary>
        public static MethodInfo AsQueryable { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Cast{TResult}" />
        /// </summary>
        public static MethodInfo Cast { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.OfType{TResult}" />
        /// </summary>
        public static MethodInfo OfType { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.All{TResult}" />
        /// </summary>
        public static MethodInfo All { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Any{TSource}(IQueryable{TSource})" />
        /// </summary>
        public static MethodInfo Any { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see cref="Queryable.Any{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo AnyWithPredicate { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Contains{TSource}(IQueryable{TSource},TSource)" />
        /// </summary>
        public static MethodInfo Contains { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Concat{TSource}" />
        /// </summary>
        public static MethodInfo Concat { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Except{TSource}(IQueryable{TSource},IEnumerable{TSource})" />
        /// </summary>
        public static MethodInfo Except { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Intersect{TSource}(IQueryable{TSource},IEnumerable{TSource})" />
        /// </summary>
        public static MethodInfo Intersect { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Union{TSource}(IQueryable{TSource},IEnumerable{TSource})" />
        /// </summary>
        public static MethodInfo Union { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Count{TSource}(IQueryable{TSource})" />
        /// </summary>
        public static MethodInfo Count { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see cref="Queryable.Count{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo CountWithPredicate { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.LongCount{TSource}(IQueryable{TSource})" />
        /// </summary>
        public static MethodInfo LongCount { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.LongCount{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo LongCountWithPredicate { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Min{TSource}" />
        /// </summary>
        public static MethodInfo MinWithSelector { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Min{TSource,TResult}(IQueryable{TSource},Expression{Func{TSource,TResult}})" />
        /// </summary>
        public static MethodInfo MinWithoutSelector { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Max{TSource}" />
        /// </summary>
        public static MethodInfo MaxWithSelector { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Max{TSource,TResult}(IQueryable{TSource},Expression{Func{TSource,TResult}})" />
        /// </summary>
        public static MethodInfo MaxWithoutSelector { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.ElementAt{TSource}" />
        /// </summary>
        public static MethodInfo ElementAt { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.ElementAtOrDefault{TSource}" />
        /// </summary>
        public static MethodInfo ElementAtOrDefault { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.First{TSource}(IQueryable{TSource})" />
        /// </summary>
        public static MethodInfo First { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.First{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo FirstWithPredicate { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.FirstOrDefault{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo FirstOrDefault { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.FirstOrDefault{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo FirstOrDefaultWithPredicate { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Single{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo Single { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Single{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo SingleWithPredicate { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.SingleOrDefault{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo SingleOrDefault { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.SingleOrDefault{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo SingleOrDefaultWithPredicate { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Last{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo Last { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Last{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo LastWithPredicate { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.LastOrDefault{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo LastOrDefault { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.LastOrDefault{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo LastOrDefaultWithPredicate { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Distinct{TSource}(IQueryable{TSource})" />
        /// </summary>
        public static MethodInfo Distinct { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Reverse{TSource}" />
        /// </summary>
        public static MethodInfo Reverse { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Where{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo Where { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see cref="Queryable.Select{TSource,TResult}(IQueryable{TSource},Expression{Func{TSource,int,TResult}})" />
        /// </summary>
        public static MethodInfo Select { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Skip{TSource}" />
        /// </summary>
        public static MethodInfo Skip { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Take{TSource}" />
        /// </summary>
        public static MethodInfo Take { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.SkipWhile{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo SkipWhile { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.TakeWhile{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public static MethodInfo TakeWhile { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.OrderBy{TSource,TKey}(IQueryable{TSource},Expression{Func{TSource,TKey}})" />
        /// </summary>
        public static MethodInfo OrderBy { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see cref="Queryable.OrderByDescending{TSource,TKey}(IQueryable{TSource},Expression{Func{TSource,TKey}})" />
        /// </summary>
        public static MethodInfo OrderByDescending { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see cref="Queryable.ThenBy{TSource,TKey}(IOrderedQueryable{TSource},Expression{Func{TSource,TKey}})" />
        /// </summary>
        public static MethodInfo ThenBy { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see cref="Queryable.ThenByDescending{TSource,TKey}(IOrderedQueryable{TSource},Expression{Func{TSource,TKey}})" />
        /// </summary>
        public static MethodInfo ThenByDescending { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.DefaultIfEmpty{TSource}(IQueryable{TSource})" />
        /// </summary>
        public static MethodInfo DefaultIfEmpty { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.DefaultIfEmpty{TSource}(IQueryable{TSource},TSource)" />
        /// </summary>
        public static MethodInfo DefaultIfEmptyWithArgument { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see
        ///         cref="Queryable.Join{TOuter,TInner,TKey,TResult}(IQueryable{TOuter},IEnumerable{TInner},Expression{Func{TOuter,TKey}},Expression{Func{TInner,TKey}},Expression{Func{TOuter,TInner,TResult}})" />
        /// </summary>
        public static MethodInfo Join { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see
        ///         cref="Queryable.GroupJoin{TOuter,TInner,TKey,TResult}(IQueryable{TOuter},IEnumerable{TInner},Expression{Func{TOuter,TKey}},Expression{Func{TInner,TKey}},Expression{Func{TOuter,IEnumerable{TInner},TResult}})" />
        /// </summary>
        public static MethodInfo GroupJoin { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see
        ///         cref="Queryable.SelectMany{TSource,TCollection,TResult}(IQueryable{TSource},Expression{Func{TSource,IEnumerable{TCollection}}},Expression{Func{TSource,TCollection,TResult}})" />
        /// </summary>
        public static MethodInfo SelectManyWithCollectionSelector { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see cref="Queryable.Select{TSource,TResult}(IQueryable{TSource},Expression{Func{TSource,int,TResult}})" />
        /// </summary>
        public static MethodInfo SelectManyWithoutCollectionSelector { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.GroupBy{TSource,TKey}(IQueryable{TSource},Expression{Func{TSource,TKey}})" />
        /// </summary>
        public static MethodInfo GroupByWithKeySelector { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see
        ///         cref="Queryable.GroupBy{TSource,TKey,TElement}(IQueryable{TSource},Expression{Func{TSource,TKey}},Expression{Func{TSource,TElement}})" />
        /// </summary>
        public static MethodInfo GroupByWithKeyElementSelector { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see
        ///         cref="Queryable.GroupBy{TSource,TKey,TElement,TResult}(IQueryable{TSource},Expression{Func{TSource,TKey}},Expression{Func{TSource,TElement}},Expression{Func{TKey,IEnumerable{TElement},TResult}})" />
        /// </summary>
        public static MethodInfo GroupByWithKeyElementResultSelector { get; }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see
        ///         cref="Queryable.GroupBy{TSource,TKey,TResult}(IQueryable{TSource},Expression{Func{TSource,TKey}},Expression{Func{TKey,IEnumerable{TSource},TResult}})" />
        /// </summary>
        public static MethodInfo GroupByWithKeyResultSelector { get; }

        /// <summary>
        ///     Checks whether or not the given <see cref="MethodInfo" /> is one of the <see cref="M:Queryable.Sum" /> without a selector.
        /// </summary>
        /// <param name="methodInfo"> The method to check. </param>
        /// <returns> <see langword="true" /> if the method matches; <see langword="false" /> otherwise. </returns>
        public static bool IsSumWithoutSelector(MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            return SumWithoutSelectorMethods.Values.Contains(methodInfo);
        }

        /// <summary>
        ///     Checks whether or not the given <see cref="MethodInfo" /> is one of the <see cref="M:Queryable.Sum" /> with a selector.
        /// </summary>
        /// <param name="methodInfo"> The method to check. </param>
        /// <returns> <see langword="true" /> if the method matches; <see langword="false" /> otherwise. </returns>
        public static bool IsSumWithSelector(MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            return methodInfo.IsGenericMethod
                && SumWithSelectorMethods.Values.Contains(methodInfo.GetGenericMethodDefinition());
        }

        /// <summary>
        ///     Checks whether or not the given <see cref="MethodInfo" /> is one of the <see cref="M:Queryable.Average" /> without a selector.
        /// </summary>
        /// <param name="methodInfo"> The method to check. </param>
        /// <returns> <see langword="true" /> if the method matches; <see langword="false" /> otherwise. </returns>
        public static bool IsAverageWithoutSelector(MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            return AverageWithoutSelectorMethods.Values.Contains(methodInfo);
        }

        /// <summary>
        ///     Checks whether or not the given <see cref="MethodInfo" /> is one of the <see cref="M:Queryable.Average" /> with a selector.
        /// </summary>
        /// <param name="methodInfo"> The method to check. </param>
        /// <returns> <see langword="true" /> if the method matches; <see langword="false" /> otherwise. </returns>
        public static bool IsAverageWithSelector(MethodInfo methodInfo)
        {
            if (methodInfo is null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            return methodInfo.IsGenericMethod
                && AverageWithSelectorMethods.Values.Contains(methodInfo.GetGenericMethodDefinition());
        }

        /// <summary>
        ///     Returns the <see cref="MethodInfo" /> for the <see cref="M:Queryable.Sum" /> method without a selector for the given type.
        /// </summary>
        /// <param name="type"> The generic type of the method to create. </param>
        /// <returns> The <see cref="MethodInfo" />. </returns>
        public static MethodInfo GetSumWithoutSelector(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return SumWithoutSelectorMethods[type];
        }

        /// <summary>
        ///     Returns the <see cref="MethodInfo" /> for the <see cref="M:Queryable.Sum" /> method with a selector for the given type.
        /// </summary>
        /// <param name="type"> The generic type of the method to create. </param>
        /// <returns> The <see cref="MethodInfo" />. </returns>
        public static MethodInfo GetSumWithSelector(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return SumWithSelectorMethods[type];
        }

        /// <summary>
        ///     Returns the <see cref="MethodInfo" /> for the <see cref="M:Queryable.Average" /> method without a selector for the given type.
        /// </summary>
        /// <param name="type"> The generic type of the method to create. </param>
        /// <returns> The <see cref="MethodInfo" />. </returns>
        public static MethodInfo GetAverageWithoutSelector(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return AverageWithoutSelectorMethods[type];
        }

        /// <summary>
        ///     Returns the <see cref="MethodInfo" /> for the <see cref="M:Queryable.Average" /> method with a selector for the given type.
        /// </summary>
        /// <param name="type"> The generic type of the method to create. </param>
        /// <returns> The <see cref="MethodInfo" />. </returns>
        public static MethodInfo GetAverageWithSelector(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return AverageWithSelectorMethods[type];
        }

        private static Dictionary<Type, MethodInfo> SumWithoutSelectorMethods { get; }
        private static Dictionary<Type, MethodInfo> SumWithSelectorMethods { get; }
        private static Dictionary<Type, MethodInfo> AverageWithoutSelectorMethods { get; }
        private static Dictionary<Type, MethodInfo> AverageWithSelectorMethods { get; }

        /// <summary>
        ///     The <see cref="RepositoryExtentions" /> for
        ///     <see
        ///         cref="RepositoryExtentions.From{TSource}(IQueryable{TSource}, Func{ITableInfo, string})" />
        /// </summary>
        public static MethodInfo From { get; }
        /// <summary>
        ///     The <see cref="RepositoryExtentions" /> for
        ///     <see
        ///         cref="RepositoryExtentions.TimeOut{TSource}(IQueryable{TSource}, int)" />
        /// </summary>
        public static MethodInfo TimeOut { get; }
        /// <summary>
        ///     The <see cref="RepositoryExtentions" /> for
        ///     <see
        ///         cref="RepositoryExtentions.NoResultError{TSource}(IQueryable{TSource}, string)" />
        /// </summary>
        public static MethodInfo NoResultError { get; }
        /// <summary>
        ///     The <see cref="RepositoryExtentions" /> for
        ///     <see
        ///         cref="RepositoryExtentions.Update{T}(IQueryable{T}, Expression{Func{T, T}})" />
        /// </summary>
        public static MethodInfo Update { get; }
        /// <summary>
        ///     The <see cref="RepositoryExtentions" /> for
        ///     <see
        ///         cref="RepositoryExtentions.Delete{T}(IQueryable{T})" />
        /// </summary>
        public static MethodInfo Delete { get; }
        /// <summary>
        ///     The <see cref="RepositoryExtentions" /> for
        ///     <see
        ///         cref="RepositoryExtentions.Delete{T}(IQueryable{T}, Expression{Func{T, bool}})" />
        /// </summary>
        public static MethodInfo DeleteWithPredicate { get; }
        /// <summary>
        ///     The <see cref="RepositoryExtentions" /> for
        ///     <see
        ///         cref="RepositoryExtentions.Insert{T}(IQueryable{T}, IQueryable{T})" />
        /// </summary>
        public static MethodInfo Insert { get; }

        static QueryableMethods()
        {
            var queryableMethods = typeof(Queryable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

            AsQueryable = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.AsQueryable) && mi.IsGenericMethod && mi.GetParameters().Length == 1);
            Cast = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Cast) && mi.GetParameters().Length == 1);
            OfType = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.OfType) && mi.GetParameters().Length == 1);

            All = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.All)
                    && mi.GetParameters().Length == 2
                    && IsExpressionOfFunc(mi.GetParameters()[1].ParameterType));
            Any = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Any) && mi.GetParameters().Length == 1);
            AnyWithPredicate = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.Any)));
            Contains = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Contains) && mi.GetParameters().Length == 2);

            Concat = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Concat) && mi.GetParameters().Length == 2);
            Except = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Except) && mi.GetParameters().Length == 2);
            Intersect = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Intersect) && mi.GetParameters().Length == 2);
            Union = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Union) && mi.GetParameters().Length == 2);

            Count = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Count) && mi.GetParameters().Length == 1);
            CountWithPredicate = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.Count)));
            LongCount = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.LongCount) && mi.GetParameters().Length == 1);
            LongCountWithPredicate = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.LongCount)));
            MinWithSelector = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.Min)));
            MinWithoutSelector = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Min) && mi.GetParameters().Length == 1);
            MaxWithSelector = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.Max)));
            MaxWithoutSelector = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Max) && mi.GetParameters().Length == 1);

            ElementAt = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.ElementAt) && mi.GetParameters().Length == 2);
            ElementAtOrDefault = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.ElementAtOrDefault) && mi.GetParameters().Length == 2);
            First = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.First) && mi.GetParameters().Length == 1);
            FirstWithPredicate = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.First)));
            FirstOrDefault = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.FirstOrDefault) && mi.GetParameters().Length == 1);
            FirstOrDefaultWithPredicate = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.FirstOrDefault)));
            Single = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Single) && mi.GetParameters().Length == 1);
            SingleWithPredicate = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.Single)));
            SingleOrDefault = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.SingleOrDefault) && mi.GetParameters().Length == 1);
            SingleOrDefaultWithPredicate = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.SingleOrDefault)));
            Last = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Last) && mi.GetParameters().Length == 1);
            LastWithPredicate = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.Last)));
            LastOrDefault = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.LastOrDefault) && mi.GetParameters().Length == 1);
            LastOrDefaultWithPredicate = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.LastOrDefault)));

            Distinct = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Distinct) && mi.GetParameters().Length == 1);
            Reverse = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Reverse) && mi.GetParameters().Length == 1);
            Where = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.Where)));
            Select = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.Select)));
            Skip = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Skip) && mi.GetParameters().Length == 2);
            Take = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Take) && mi.GetParameters().Length == 2);
            SkipWhile = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.SkipWhile)));
            TakeWhile = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.TakeWhile)));
            OrderBy = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.OrderBy)));
            OrderByDescending = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.OrderByDescending)));
            ThenBy = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.ThenBy)));
            ThenByDescending = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.ThenByDescending)));
            DefaultIfEmpty = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.DefaultIfEmpty) && mi.GetParameters().Length == 1);
            DefaultIfEmptyWithArgument = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.DefaultIfEmpty) && mi.GetParameters().Length == 2);

            Join = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.Join) && mi.GetParameters().Length == 5);
            GroupJoin = queryableMethods.Single(
                mi => mi.Name == nameof(Queryable.GroupJoin) && mi.GetParameters().Length == 5);
            SelectManyWithCollectionSelector = queryableMethods.Single(
                mi =>
                {
                    if (mi.Name != nameof(Queryable.SelectMany))
                    {
                        return false;
                    }

                    var parameters = mi.GetParameters();

                    return parameters.Length == 3
                        && IsExpressionOfFunc(parameters[1].ParameterType);
                });
            SelectManyWithoutCollectionSelector = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.SelectMany)));

            GroupByWithKeySelector = queryableMethods.Single(
                mi => IsTwoArgsWithLambda(mi, nameof(Queryable.GroupBy)));
            GroupByWithKeyElementSelector = queryableMethods.Single(
                mi =>
                {
                    if (mi.Name != nameof(Queryable.GroupBy))
                    {
                        return false;
                    }

                    var parameters = mi.GetParameters();

                    return parameters.Length == 3
                        && IsExpressionOfFunc(parameters[1].ParameterType)
                        && IsExpressionOfFunc(parameters[2].ParameterType);
                });
            GroupByWithKeyElementResultSelector = queryableMethods.Single(
                mi =>
                {
                    if (mi.Name != nameof(Queryable.GroupBy))
                    {
                        return false;
                    }

                    var parameters = mi.GetParameters();

                    return parameters.Length == 4
                        && IsExpressionOfFunc(parameters[1].ParameterType)
                        && IsExpressionOfFunc(parameters[2].ParameterType)
                        && IsExpressionOfFunc(parameters[3].ParameterType, 3);
                });
            GroupByWithKeyResultSelector = queryableMethods.Single(
                mi =>
                {
                    if (mi.Name != nameof(Queryable.GroupBy))
                    {
                        return false;
                    }

                    var parameters = mi.GetParameters();

                    return parameters.Length == 3
                        && IsExpressionOfFunc(parameters[1].ParameterType)
                        && IsExpressionOfFunc(parameters[2].ParameterType, 3);
                });

            SumWithoutSelectorMethods = new Dictionary<Type, MethodInfo>
            {
                { typeof(decimal), GetSumOrAverageWithoutSelector<decimal>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(long), GetSumOrAverageWithoutSelector<long>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(int), GetSumOrAverageWithoutSelector<int>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(double), GetSumOrAverageWithoutSelector<double>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(float), GetSumOrAverageWithoutSelector<float>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(decimal?), GetSumOrAverageWithoutSelector<decimal?>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(long?), GetSumOrAverageWithoutSelector<long?>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(int?), GetSumOrAverageWithoutSelector<int?>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(double?), GetSumOrAverageWithoutSelector<double?>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(float?), GetSumOrAverageWithoutSelector<float?>(queryableMethods, nameof(Queryable.Sum)) }
            };

            SumWithSelectorMethods = new Dictionary<Type, MethodInfo>
            {
                { typeof(decimal), GetSumOrAverageWithSelector<decimal>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(long), GetSumOrAverageWithSelector<long>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(int), GetSumOrAverageWithSelector<int>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(double), GetSumOrAverageWithSelector<double>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(float), GetSumOrAverageWithSelector<float>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(decimal?), GetSumOrAverageWithSelector<decimal?>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(long?), GetSumOrAverageWithSelector<long?>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(int?), GetSumOrAverageWithSelector<int?>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(double?), GetSumOrAverageWithSelector<double?>(queryableMethods, nameof(Queryable.Sum)) },
                { typeof(float?), GetSumOrAverageWithSelector<float?>(queryableMethods, nameof(Queryable.Sum)) }
            };

            AverageWithoutSelectorMethods = new Dictionary<Type, MethodInfo>
            {
                { typeof(decimal), GetSumOrAverageWithoutSelector<decimal>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(long), GetSumOrAverageWithoutSelector<long>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(int), GetSumOrAverageWithoutSelector<int>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(double), GetSumOrAverageWithoutSelector<double>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(float), GetSumOrAverageWithoutSelector<float>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(decimal?), GetSumOrAverageWithoutSelector<decimal?>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(long?), GetSumOrAverageWithoutSelector<long?>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(int?), GetSumOrAverageWithoutSelector<int?>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(double?), GetSumOrAverageWithoutSelector<double?>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(float?), GetSumOrAverageWithoutSelector<float?>(queryableMethods, nameof(Queryable.Average)) }
            };

            AverageWithSelectorMethods = new Dictionary<Type, MethodInfo>
            {
                { typeof(decimal), GetSumOrAverageWithSelector<decimal>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(long), GetSumOrAverageWithSelector<long>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(int), GetSumOrAverageWithSelector<int>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(double), GetSumOrAverageWithSelector<double>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(float), GetSumOrAverageWithSelector<float>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(decimal?), GetSumOrAverageWithSelector<decimal?>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(long?), GetSumOrAverageWithSelector<long?>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(int?), GetSumOrAverageWithSelector<int?>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(double?), GetSumOrAverageWithSelector<double?>(queryableMethods, nameof(Queryable.Average)) },
                { typeof(float?), GetSumOrAverageWithSelector<float?>(queryableMethods, nameof(Queryable.Average)) }
            };

            MethodInfo GetSumOrAverageWithoutSelector<T>(MethodInfo[] queryableMethod2s, string methodName)
               => queryableMethod2s.Single(
                   mi =>
                   {
                       if (mi.Name != methodName)
                       {
                           return false;
                       }

                       var parameters = mi.GetParameters();

                       if (parameters.Length != 1)
                       {
                           return false;
                       }

                       return parameters[0].ParameterType.GetGenericArguments()[0] == typeof(T);
                   });

            MethodInfo GetSumOrAverageWithSelector<T>(MethodInfo[] queryableMethod2s, string methodName)
               => queryableMethod2s.Single(
                   mi =>
                   {
                       if (mi.Name != methodName)
                       {
                           return false;
                       }

                       var parameters = mi.GetParameters();

                       if (parameters.Length != 2)
                       {
                           return false;
                       }

                       return IsSelector<T>(parameters[1].ParameterType);
                   });

            bool IsExpressionOfFunc(Type type, int funcGenericArgs = 2)
            {
                if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Expression<>))
                {
                    return false;
                }

                var typeArguments = type.GetGenericArguments();

                var typeArgument = typeArguments[0];

                return typeArgument.IsGenericType && typeArgument.GetGenericArguments().Length == funcGenericArgs;
            };

            bool IsSelector<T>(Type type)
            {
                if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Expression<>))
                {
                    return false;
                }

                var typeArguments = type.GetGenericArguments();

                var typeArgument = typeArguments[0];

                if (!typeArgument.IsGenericType)
                {
                    return false;
                }

                typeArguments = typeArgument.GetGenericArguments();

                if (typeArguments.Length != 2)
                {
                    return false;
                }

                return typeArguments[1] == typeof(T);
            }

            bool IsTwoArgsWithLambda(MethodInfo mi, string methodName)
            {
                if (mi.Name != methodName)
                {
                    return false;
                }

                var parameters = mi.GetParameters();

                return parameters.Length == 2 && IsExpressionOfFunc(parameters[1].ParameterType);
            }

            var repositoryMethods = typeof(RepositoryExtentions)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

            From = repositoryMethods.Single(mi => mi.Name == nameof(RepositoryExtentions.From));
            TimeOut = repositoryMethods.Single(mi => mi.Name == nameof(RepositoryExtentions.TimeOut));
            NoResultError = repositoryMethods.Single(mi => mi.Name == nameof(RepositoryExtentions.NoResultError));
            Insert = repositoryMethods.Single(mi => mi.Name == nameof(RepositoryExtentions.Insert));
            Update = repositoryMethods.Single(mi => mi.Name == nameof(RepositoryExtentions.Update));
            Delete = repositoryMethods.Single(mi => mi.Name == nameof(RepositoryExtentions.Delete) && mi.GetParameters().Length == 1);
            DeleteWithPredicate = repositoryMethods.Single(mi => mi.Name == nameof(RepositoryExtentions.Delete) && mi.GetParameters().Length == 2);
        }
    }
}

#if NET_NORMAL || NET_CORE
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq
{
    /// <summary>
    /// 异步查询。
    /// </summary>
    public static class QueryableAsync
    {
        #region Any/All

        /// <summary>
        ///     Asynchronously determines whether a sequence contains any elements.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to check for being empty.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains <see langword="true" /> if the source sequence contains any elements; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<bool> AnyAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, bool>(QueryableMethods.Any, source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously determines whether any element of a sequence satisfies a condition.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> whose elements to test for a condition.
        /// </param>
        /// <param name="predicate"> A function to test each element for a condition. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains <see langword="true" /> if any elements in the source sequence pass the test in the specified
        ///     predicate; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
        /// </exception>
        public static Task<bool> AnyAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, bool>(QueryableMethods.AnyWithPredicate, source, predicate, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously determines whether all the elements of a sequence satisfy a condition.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> whose elements to test for a condition.
        /// </param>
        /// <param name="predicate"> A function to test each element for a condition. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains <see langword="true" /> if every element of the source sequence passes the test in the specified
        ///     predicate; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
        /// </exception>
        public static Task<bool> AllAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, bool>(QueryableMethods.All, source, predicate, cancellationToken);
        }

        #endregion

        #region Count/LongCount

        /// <summary>
        ///     Asynchronously returns the number of elements in a sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> that contains the elements to be counted.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the number of elements in the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<int> CountAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, int>(QueryableMethods.Count, source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns the number of elements in a sequence that satisfy a condition.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> that contains the elements to be counted.
        /// </param>
        /// <param name="predicate"> A function to test each element for a condition. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the number of elements in the sequence that satisfy the condition in the predicate
        ///     function.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
        /// </exception>
        public static Task<int> CountAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, int>(QueryableMethods.CountWithPredicate, source, predicate, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns an <see cref="long" /> that represents the total number of elements in a sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> that contains the elements to be counted.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the number of elements in the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<long> LongCountAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, long>(QueryableMethods.LongCount, source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns an <see cref="long" /> that represents the number of elements in a sequence
        ///     that satisfy a condition.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> that contains the elements to be counted.
        /// </param>
        /// <param name="predicate"> A function to test each element for a condition. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the number of elements in the sequence that satisfy the condition in the predicate
        ///     function.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
        /// </exception>
        public static Task<long> LongCountAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, long>(QueryableMethods.LongCountWithPredicate, source, predicate, cancellationToken);
        }

        #endregion

        #region First/FirstOrDefault

        /// <summary>
        ///     Asynchronously returns the first element of a sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the first element of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the first element in <paramref name="source" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<TSource> FirstAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.First, source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns the first element of a sequence that satisfies a specified condition.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the first element of.
        /// </param>
        /// <param name="predicate"> A function to test each element for a condition. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the first element in <paramref name="source" /> that passes the test in
        ///     <paramref name="predicate" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para>
        ///         No element satisfies the condition in <paramref name="predicate" />
        ///     </para>
        ///     <para>
        ///         -or -
        ///     </para>
        ///     <para>
        ///         <paramref name="source" /> contains no elements.
        ///     </para>
        /// </exception>
        public static Task<TSource> FirstAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.FirstWithPredicate, source, predicate, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns the first element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the first element of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains <see langword="default" /> ( <typeparamref name="TSource" /> ) if
        ///     <paramref name="source" /> is empty; otherwise, the first element in <paramref name="source" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<TSource> FirstOrDefaultAsync<TSource>(
           this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.FirstOrDefault, source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns the first element of a sequence that satisfies a specified condition
        ///     or a default value if no such element is found.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the first element of.
        /// </param>
        /// <param name="predicate"> A function to test each element for a condition. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains <see langword="default" /> ( <typeparamref name="TSource" /> ) if <paramref name="source" />
        ///     is empty or if no element passes the test specified by <paramref name="predicate" /> ; otherwise, the first
        ///     element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
        /// </exception>
        public static Task<TSource> FirstOrDefaultAsync<TSource>(
           this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.FirstOrDefaultWithPredicate, source, predicate, cancellationToken);
        }

        #endregion

        #region Last/LastOrDefault

        /// <summary>
        ///     Asynchronously returns the last element of a sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the last element of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the last element in <paramref name="source" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<TSource> LastAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.Last, source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns the last element of a sequence that satisfies a specified condition.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the last element of.
        /// </param>
        /// <param name="predicate"> A function to test each element for a condition. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the last element in <paramref name="source" /> that passes the test in
        ///     <paramref name="predicate" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para>
        ///         No element satisfies the condition in <paramref name="predicate" />.
        ///     </para>
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <para>
        ///         <paramref name="source" /> contains no elements.
        ///     </para>
        /// </exception>
        public static Task<TSource> LastAsync<TSource>(
             this IQueryable<TSource> source,
             Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.LastWithPredicate, source, predicate, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns the last element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the last element of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains <see langword="default" /> ( <typeparamref name="TSource" /> ) if
        ///     <paramref name="source" /> is empty; otherwise, the last element in <paramref name="source" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<TSource> LastOrDefaultAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.LastOrDefault, source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns the last element of a sequence that satisfies a specified condition
        ///     or a default value if no such element is found.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the last element of.
        /// </param>
        /// <param name="predicate"> A function to test each element for a condition. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains <see langword="default" /> ( <typeparamref name="TSource" /> ) if <paramref name="source" />
        ///     is empty or if no element passes the test specified by <paramref name="predicate" /> ; otherwise, the last
        ///     element in <paramref name="source" /> that passes the test specified by <paramref name="predicate" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
        /// </exception>
        public static Task<TSource> LastOrDefaultAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.LastOrDefaultWithPredicate, source, predicate, cancellationToken);
        }

        #endregion

        #region Single/SingleOrDefault

        /// <summary>
        ///     Asynchronously returns the only element of a sequence, and throws an exception
        ///     if there is not exactly one element in the sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the single element of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the single element of the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para>
        ///         <paramref name="source" /> contains more than one elements.
        ///     </para>
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <para>
        ///         <paramref name="source" /> contains no elements.
        ///     </para>
        /// </exception>
        public static Task<TSource> SingleAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.Single, source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns the only element of a sequence that satisfies a specified condition,
        ///     and throws an exception if more than one such element exists.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the single element of.
        /// </param>
        /// <param name="predicate"> A function to test an element for a condition. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the single element of the input sequence that satisfies the condition in
        ///     <paramref name="predicate" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <para>
        ///         No element satisfies the condition in <paramref name="predicate" />.
        ///     </para>
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <para>
        ///         More than one element satisfies the condition in <paramref name="predicate" />.
        ///     </para>
        ///     <para>
        ///         -or-
        ///     </para>
        ///     <para>
        ///         <paramref name="source" /> contains no elements.
        ///     </para>
        /// </exception>
        public static Task<TSource> SingleAsync<TSource>(
             this IQueryable<TSource> source,
             Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.SingleWithPredicate, source, predicate, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns the only element of a sequence, or a default value if the sequence is empty;
        ///     this method throws an exception if there is more than one element in the sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the single element of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the single element of the input sequence, or <see langword="default" /> (
        ///     <typeparamref name="TSource" />)
        ///     if the sequence contains no elements.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains more than one element.
        /// </exception>
        public static Task<TSource> SingleOrDefaultAsync<TSource>(
             this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.SingleOrDefault, source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously returns the only element of a sequence that satisfies a specified condition or
        ///     a default value if no such element exists; this method throws an exception if more than one element
        ///     satisfies the condition.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the single element of.
        /// </param>
        /// <param name="predicate"> A function to test an element for a condition. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the single element of the input sequence that satisfies the condition in
        ///     <paramref name="predicate" />, or <see langword="default" /> ( <typeparamref name="TSource" /> ) if no such element is found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="predicate" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     More than one element satisfies the condition in <paramref name="predicate" />.
        /// </exception>
        public static Task<TSource> SingleOrDefaultAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(
                QueryableMethods.SingleOrDefaultWithPredicate, source, predicate, cancellationToken);
        }

        #endregion

        #region Min

        /// <summary>
        ///     Asynchronously returns the minimum value of a sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> that contains the elements to determine the minimum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the minimum value in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<TSource> MinAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.MinWithoutSelector, source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously invokes a projection function on each element of a sequence and returns the minimum resulting value.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <typeparam name="TResult">
        ///     The type of the value returned by the function represented by <paramref name="selector" /> .
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> that contains the elements to determine the minimum of.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the minimum value in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<TResult> MinAsync<TSource, TResult>(
             this IQueryable<TSource> source,
             Expression<Func<TSource, TResult>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TResult>(QueryableMethods.MinWithSelector, source, selector, cancellationToken);
        }

        #endregion

        #region Max

        /// <summary>
        ///     Asynchronously returns the maximum value of a sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> that contains the elements to determine the maximum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the maximum value in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<TSource> MaxAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TSource>(QueryableMethods.MaxWithoutSelector, source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously invokes a projection function on each element of a sequence and returns the maximum resulting value.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <typeparam name="TResult">
        ///     The type of the value returned by the function represented by <paramref name="selector" /> .
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> that contains the elements to determine the maximum of.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the maximum value in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<TResult> MaxAsync<TSource, TResult>(
             this IQueryable<TSource> source,
             Expression<Func<TSource, TResult>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, TResult>(QueryableMethods.MaxWithSelector, source, selector, cancellationToken);
        }

        #endregion

        #region Sum

        /// <summary>
        ///     Asynchronously computes the sum of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the sum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the values in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<decimal> SumAsync(
            this IQueryable<decimal> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<decimal, decimal>(QueryableMethods.GetSumWithoutSelector(typeof(decimal)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the sum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the values in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<decimal?> SumAsync(
             this IQueryable<decimal?> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<decimal?, decimal?>(
                QueryableMethods.GetSumWithoutSelector(typeof(decimal?)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
        ///     each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values of type <typeparamref name="TSource" />.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the projected values..
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<decimal> SumAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, decimal>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, decimal>(
                QueryableMethods.GetSumWithSelector(typeof(decimal)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
        ///     each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values of type <typeparamref name="TSource" />.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the projected values..
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<decimal?> SumAsync<TSource>(
            this IQueryable<TSource> source,
             Expression<Func<TSource, decimal?>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, decimal?>(
                QueryableMethods.GetSumWithSelector(typeof(decimal?)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the sum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the values in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<int> SumAsync(
            this IQueryable<int> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<int, int>(QueryableMethods.GetSumWithoutSelector(typeof(int)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the sum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the values in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<int?> SumAsync(
            this IQueryable<int?> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<int?, int?>(QueryableMethods.GetSumWithoutSelector(typeof(int?)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
        ///     each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values of type <typeparamref name="TSource" />.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the projected values..
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<int> SumAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, int>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, int>(QueryableMethods.GetSumWithSelector(typeof(int)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
        ///     each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values of type <typeparamref name="TSource" />.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the projected values..
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<int?> SumAsync<TSource>(
             this IQueryable<TSource> source,
             Expression<Func<TSource, int?>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, int?>(
                QueryableMethods.GetSumWithSelector(typeof(int?)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the sum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the values in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<long> SumAsync(
            this IQueryable<long> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<long, long>(QueryableMethods.GetSumWithoutSelector(typeof(long)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the sum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the values in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<long?> SumAsync(
            this IQueryable<long?> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<long?, long?>(QueryableMethods.GetSumWithoutSelector(typeof(long?)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
        ///     each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values of type <typeparamref name="TSource" />.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the projected values..
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<long> SumAsync<TSource>(
             this IQueryable<TSource> source,
             Expression<Func<TSource, long>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, long>(
                QueryableMethods.GetSumWithSelector(typeof(long)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
        ///     each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values of type <typeparamref name="TSource" />.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the projected values..
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<long?> SumAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, long?>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, long?>(
                QueryableMethods.GetSumWithSelector(typeof(long?)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the sum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the values in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<double> SumAsync(
            this IQueryable<double> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<double, double>(QueryableMethods.GetSumWithoutSelector(typeof(double)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the sum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the values in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<double?> SumAsync(
            this IQueryable<double?> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<double?, double?>(QueryableMethods.GetSumWithoutSelector(typeof(double?)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
        ///     each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values of type <typeparamref name="TSource" />.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the projected values..
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<double> SumAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, double>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, double>(
                QueryableMethods.GetSumWithSelector(typeof(double)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
        ///     each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values of type <typeparamref name="TSource" />.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the projected values..
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<double?> SumAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, double?>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, double?>(
                QueryableMethods.GetSumWithSelector(typeof(double?)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the sum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the values in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<float> SumAsync(
            this IQueryable<float> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<float, float>(QueryableMethods.GetSumWithoutSelector(typeof(float)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the sum of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the values in the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<float?> SumAsync(
            this IQueryable<float?> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<float?, float?>(QueryableMethods.GetSumWithoutSelector(typeof(float?)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
        ///     each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values of type <typeparamref name="TSource" />.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the projected values..
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<float> SumAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, float>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, float>(
                QueryableMethods.GetSumWithSelector(typeof(float)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the sum of the sequence of values that is obtained by invoking a projection function on
        ///     each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values of type <typeparamref name="TSource" />.
        /// </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the sum of the projected values..
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<float?> SumAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, float?>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, float?>(
                QueryableMethods.GetSumWithSelector(typeof(float?)), source, selector, cancellationToken);
        }

        #endregion

        #region Average

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the average of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the sequence of values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<decimal> AverageAsync(
            this IQueryable<decimal> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<decimal, decimal>(
                QueryableMethods.GetAverageWithoutSelector(typeof(decimal)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the average of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the sequence of values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<decimal?> AverageAsync(
            this IQueryable<decimal?> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<decimal?, decimal?>(
                QueryableMethods.GetAverageWithoutSelector(typeof(decimal?)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values that is obtained
        ///     by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" /> .
        /// </typeparam>
        /// <param name="source"> A sequence of values of type <typeparamref name="TSource" />. </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the projected values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<decimal> AverageAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, decimal>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, decimal>(
                QueryableMethods.GetAverageWithSelector(typeof(decimal)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values that is obtained
        ///     by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" /> .
        /// </typeparam>
        /// <param name="source"> A sequence of values of type <typeparamref name="TSource" />. </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the projected values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<decimal?> AverageAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, decimal?>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, decimal?>(
                QueryableMethods.GetAverageWithSelector(typeof(decimal?)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the average of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the sequence of values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<double> AverageAsync(
            this IQueryable<int> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<int, double>(QueryableMethods.GetAverageWithoutSelector(typeof(int)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the average of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the sequence of values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<double?> AverageAsync(
            this IQueryable<int?> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<int?, double?>(QueryableMethods.GetAverageWithoutSelector(typeof(int?)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values that is obtained
        ///     by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" /> .
        /// </typeparam>
        /// <param name="source"> A sequence of values of type <typeparamref name="TSource" />. </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the projected values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<double> AverageAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, int>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, double>(
                QueryableMethods.GetAverageWithSelector(typeof(int)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values that is obtained
        ///     by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" /> .
        /// </typeparam>
        /// <param name="source"> A sequence of values of type <typeparamref name="TSource" />. </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the projected values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<double?> AverageAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, int?>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, double?>(
                QueryableMethods.GetAverageWithSelector(typeof(int?)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the average of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the sequence of values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<double> AverageAsync(
            this IQueryable<long> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<long, double>(QueryableMethods.GetAverageWithoutSelector(typeof(long)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the average of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the sequence of values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<double?> AverageAsync(
            this IQueryable<long?> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<long?, double?>(QueryableMethods.GetAverageWithoutSelector(typeof(long?)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values that is obtained
        ///     by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" /> .
        /// </typeparam>
        /// <param name="source"> A sequence of values of type <typeparamref name="TSource" />. </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the projected values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<double> AverageAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, long>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, double>(
                QueryableMethods.GetAverageWithSelector(typeof(long)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values that is obtained
        ///     by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" /> .
        /// </typeparam>
        /// <param name="source"> A sequence of values of type <typeparamref name="TSource" />. </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the projected values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<double?> AverageAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, long?>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, double?>(
                QueryableMethods.GetAverageWithSelector(typeof(long?)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the average of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the sequence of values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<double> AverageAsync(
            this IQueryable<double> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<double, double>(
                QueryableMethods.GetAverageWithoutSelector(typeof(double)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the average of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the sequence of values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<double?> AverageAsync(
            this IQueryable<double?> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<double?, double?>(
                QueryableMethods.GetAverageWithoutSelector(typeof(double?)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values that is obtained
        ///     by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" /> .
        /// </typeparam>
        /// <param name="source"> A sequence of values of type <typeparamref name="TSource" />. </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the projected values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<double> AverageAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, double>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, double>(
                QueryableMethods.GetAverageWithSelector(typeof(double)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values that is obtained
        ///     by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" /> .
        /// </typeparam>
        /// <param name="source"> A sequence of values of type <typeparamref name="TSource" />. </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the projected values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<double?> AverageAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, double?>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, double?>(
                QueryableMethods.GetAverageWithSelector(typeof(double?)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the average of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the sequence of values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<float> AverageAsync(
            this IQueryable<float> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<float, float>(QueryableMethods.GetAverageWithoutSelector(typeof(float)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <param name="source">
        ///     A sequence of values to calculate the average of.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the sequence of values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<float?> AverageAsync(
            this IQueryable<float?> source,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<float?, float?>(
                QueryableMethods.GetAverageWithoutSelector(typeof(float?)), source, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values that is obtained
        ///     by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" /> .
        /// </typeparam>
        /// <param name="source"> A sequence of values of type <typeparamref name="TSource" />. </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the projected values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> contains no elements.
        /// </exception>
        public static Task<float> AverageAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, float>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, float>(
                QueryableMethods.GetAverageWithSelector(typeof(float)), source, selector, cancellationToken);
        }

        /// <summary>
        ///     Asynchronously computes the average of a sequence of values that is obtained
        ///     by invoking a projection function on each element of the input sequence.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" /> .
        /// </typeparam>
        /// <param name="source"> A sequence of values of type <typeparamref name="TSource" />. </param>
        /// <param name="selector"> A projection function to apply to each element. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains the average of the projected values.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
        /// </exception>
        public static Task<float?> AverageAsync<TSource>(
            this IQueryable<TSource> source,
            Expression<Func<TSource, float?>> selector,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, float?>(
                QueryableMethods.GetAverageWithSelector(typeof(float?)), source, selector, cancellationToken);
        }

        #endregion

        #region Contains

        /// <summary>
        ///     Asynchronously determines whether a sequence contains a specified element by using the default equality comparer.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to return the single element of.
        /// </param>
        /// <param name="item"> The object to locate in the sequence. </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains <see langword="true" /> if the input sequence contains the specified value; otherwise,
        ///     <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static Task<bool> ContainsAsync<TSource>(
            this IQueryable<TSource> source,
            TSource item,
            CancellationToken cancellationToken = default)
        {
            return ExecuteAsync<TSource, bool>(
                QueryableMethods.Contains,
                source,
                Expression.Constant(item, typeof(TSource)),
                cancellationToken);
        }

        #endregion

        #region ToList/Array

        /// <summary>
        ///     Asynchronously creates a <see cref="List{T}" /> from an <see cref="IQueryable{T}" /> by enumerating it
        ///     asynchronously.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to create a list from.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains a <see cref="List{T}" /> that contains elements from the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static async Task<List<TSource>> ToListAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default) => await source.AsAsyncEnumerable().ToListAsync(cancellationToken);

        /// <summary>
        ///     Asynchronously creates an array from an <see cref="IQueryable{T}" /> by enumerating it asynchronously.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to create an array from.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A task that represents the asynchronous operation.
        ///     The task result contains an array that contains elements from the input sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static async Task<TSource[]> ToArrayAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
            => await source.AsAsyncEnumerable().ToArrayAsync(cancellationToken);

        /// <summary>
        /// Asynchronously enumerates the query results and performs the specified action on each element.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An System.Linq.IQueryable`1 to enumerate.</param>
        /// <param name="action">The action to perform on each element.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task ForEachAsync<TSource>(this IQueryable<TSource> source, Action<TSource> action, CancellationToken cancellationToken = default)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var enumerator = source
                .AsAsyncEnumerable()
                .GetAsyncEnumerator(cancellationToken);

            while (await enumerator.MoveNextAsync())
            {
                action.Invoke(enumerator.Current);
            }
        }

        #endregion

        #region AsAsyncEnumerable

        /// <summary>
        ///     Returns an <see cref="IAsyncEnumerable{T}" /> which can be enumerated asynchronously.
        /// </summary>
        /// <remarks>
        ///     Multiple active operations on the same context instance are not supported.  Use 'await' to ensure
        ///     that any asynchronous operations have completed before calling another method on this context.
        /// </remarks>
        /// <typeparam name="TSource">
        ///     The type of the elements of <paramref name="source" />.
        /// </typeparam>
        /// <param name="source">
        ///     An <see cref="IQueryable{T}" /> to enumerate.
        /// </param>
        /// <returns> The query results. </returns>
        /// <exception cref="InvalidOperationException">
        ///     <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="source" /> is not a <see cref="IAsyncEnumerable{T}" />.
        /// </exception>
        public static IAsyncEnumerable<TSource> AsAsyncEnumerable<TSource>(this IQueryable<TSource> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is IAsyncEnumerable<TSource> asyncEnumerable)
            {
                return asyncEnumerable;
            }

            throw new InvalidOperationException();
        }

        #endregion

        #region Impl.

        private static Task<TResult> ExecuteAsync<TSource, TResult>(
            MethodInfo operatorMethodInfo,
            IQueryable<TSource> source,
            Expression expression,
            CancellationToken cancellationToken = default)
        {
            if (source.Provider is IAsyncQueryProvider provider)
            {
                if (operatorMethodInfo.IsGenericMethod)
                {
                    operatorMethodInfo
                        = operatorMethodInfo.GetGenericArguments().Length == 2
                            ? operatorMethodInfo.MakeGenericMethod(typeof(TSource), typeof(TResult))
                            : operatorMethodInfo.MakeGenericMethod(typeof(TSource));
                }

                return provider.ExecuteAsync<TResult>(
                    Expression.Call(
                        instance: null,
                        method: operatorMethodInfo,
                        arguments: expression is null
                            ? new Expression[1] { source.Expression }
                            : new Expression[2] { source.Expression, expression }),
                    cancellationToken);
            }

            throw new InvalidOperationException();
        }

        private static Task<TResult> ExecuteAsync<TSource, TResult>(
            MethodInfo operatorMethodInfo,
            IQueryable<TSource> source,
            LambdaExpression expression,
            CancellationToken cancellationToken = default)
            => ExecuteAsync<TSource, TResult>(
                operatorMethodInfo, source, Expression.Quote(expression), cancellationToken);

        private static Task<TResult> ExecuteAsync<TSource, TResult>(
            MethodInfo operatorMethodInfo,
            IQueryable<TSource> source,
            CancellationToken cancellationToken = default)
            => ExecuteAsync<TSource, TResult>(
                operatorMethodInfo, source, (Expression)null, cancellationToken);

        #endregion
    }
}
#endif
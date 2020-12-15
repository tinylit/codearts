using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// Defines the interface(s) for unit of work.
    /// </summary>
    public interface IDbTransaction : IDisposable
    {
#if NETSTANDARD2_0
        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>        
        /// <param name="acceptAllChangesOnSuccess">Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <returns>The number of state entries written to the database.</returns>
        int Commit(bool acceptAllChangesOnSuccess = false);
#else
        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        int Commit();
#endif

#if NET_NORMAL || NETSTANDARD2_0
        /// <summary>
        /// Asynchronously saves all changes made in this unit of work to the database.
        /// </summary>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        Task<int> CommitAsync(CancellationToken cancellationToken = default);
#endif

#if NETSTANDARD2_0
        /// <summary>
        /// Asynchronously saves all changes made in this unit of work to the database.
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous save operation. The task result contains the number of state entities written to database.</returns>
        Task<int> CommitAsync(bool acceptAllChangesOnSuccess = false, CancellationToken cancellationToken = default);
#endif
    }
}

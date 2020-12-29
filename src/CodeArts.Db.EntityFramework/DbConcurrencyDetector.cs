#if NET_CORE
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;

namespace CodeArts.Db.EntityFramework
{
    /// <summary>
    /// 并发检测器。
    /// </summary>
    public class DbConcurrencyDetector : IConcurrencyDetector
    {
#if NETSTANDARD2_1
        /// <summary>
        /// inheritdoc.
        /// </summary>
        /// <returns></returns>
        public ConcurrencyDetectorCriticalSectionDisposer EnterCriticalSection() => new ConcurrencyDetectorCriticalSectionDisposer(this);
        
        /// <summary>
        /// inheritdoc.
        /// </summary>
        public void ExitCriticalSection()
        {
        }

#else
        private class Disposable : IDisposable
        {
            public void Dispose() { }
        }
        /// <summary>
        /// inheritdoc.
        /// </summary>
        /// <returns></returns>
        IDisposable IConcurrencyDetector.EnterCriticalSection() => new Disposable();
#endif
    }
}
#endif
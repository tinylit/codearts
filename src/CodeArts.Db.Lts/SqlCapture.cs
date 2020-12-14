using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// SQL 捕获器。
    /// </summary>
    public class SqlCapture : IDisposable
    {
        private readonly SqlCapture capture;

        private readonly static Dictionary<Thread, SqlCapture> threadSqlProfilers = new Dictionary<Thread, SqlCapture>();

        /// <summary>
        /// 捕获器。
        /// </summary>
        public SqlCapture()
        {
            var thread = Thread.CurrentThread;

            lock (threadSqlProfilers)
            {
                if (threadSqlProfilers.TryGetValue(thread, out SqlCapture capture))
                {
                    this.capture = capture;
                }
            }
            threadSqlProfilers[thread] = this;
        }

        /// <summary>
        /// 当前捕获器。
        /// </summary>
        public static SqlCapture Current
        {
            get
            {
                if (threadSqlProfilers.TryGetValue(Thread.CurrentThread, out SqlCapture capture))
                {
                    return capture;
                }

                return default;
            }
        }

        /// <summary>
        /// 已捕获。
        /// </summary>
        public Action<CommandSql> Captured { get; set; }

        /// <summary>
        /// 捕获。
        /// </summary>
        /// <param name="commandSql">命令SQL。</param>
        public virtual void Capture(CommandSql commandSql)
        {
            Captured?.Invoke(commandSql);

            capture?.Captured?.Invoke(commandSql);
        }

        /// <summary>
        /// 释放资源。
        /// </summary>
        public void Dispose()
        {
            var thread = Thread.CurrentThread;

            lock (threadSqlProfilers)
            {
                threadSqlProfilers.Remove(thread);
            }

            if (capture is null)
            {
                return;
            }

            lock (threadSqlProfilers)
            {
                threadSqlProfilers[thread] = capture;
            }
        }
    }
}

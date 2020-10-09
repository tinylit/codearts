using System;
using System.Collections.Generic;
using System.Threading;

namespace CodeArts.ORM
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
        public Action<SqlCaptureContext> Captured { get; set; }

        /// <summary>
        /// 捕获。
        /// </summary>
        /// <param name="sql">语句。</param>
        /// <param name="parameters">参数。</param>
        /// <param name="behavior">行为。</param>
        public virtual void Capture(string sql, Dictionary<string, object> parameters, CommandBehavior behavior)
        {
            var context = new SqlCaptureContext(sql, parameters, behavior);

            Captured?.Invoke(context);

            capture?.Captured?.Invoke(context);
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

    /// <summary>
    /// SQL 捕获上下文。
    /// </summary>
    public class SqlCaptureContext
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public SqlCaptureContext(string sql, Dictionary<string, object> parameters, CommandBehavior behavior)
        {
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentException($"“{nameof(sql)}”不能是 Null 或为空。", nameof(sql));
            }

            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            Sql = sql;
            Parameters = parameters;
            Behavior = behavior;
        }

        /// <summary>
        /// 语句。
        /// </summary>
        public string Sql { get; }

        /// <summary>
        /// 参数。
        /// </summary>
        public Dictionary<string, object> Parameters { get; }

        /// <summary>
        /// 行为。
        /// </summary>
        public CommandBehavior Behavior { get; }
    }
}

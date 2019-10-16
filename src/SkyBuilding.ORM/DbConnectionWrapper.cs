using System;
using System.Data;
using System.Threading;
using System.Transactions;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// 数据库连接的基本信息
    /// </summary>
    public class DbConnectionWrapper : DbConnection
    {
        private int _refCount = 0;                  //当前事务中，当前还有多少个活跃连接
        private DateTime _lasteUsedTime;            //最近一次活跃时间
        private readonly bool _isTransaction;       //是否处于事务中
        private readonly TimeSpan _aliveTime;       //连接有效时间，默认有效时间30分钟，超过30分钟则认为无效


        /// <summary> 构造函数 </summary>
        /// <param name="connection">数据库连接</param>
        /// <param name="createTime">创建时间</param>
        /// <param name="aliveTime">存活时间</param>
        public DbConnectionWrapper(IDbConnection connection, TimeSpan? aliveTime = null) : base(connection)
        {
            _isTransaction = !(Transaction.Current is null);
            _aliveTime = aliveTime ?? TimeSpan.FromMinutes(5D);
        }

        /// <summary> 获取连接 </summary>
        /// <returns></returns>
        public IDbConnection GetConnection()
        {
            if (_isTransaction)
            {
                Interlocked.Increment(ref _refCount);
            }
            return this;
        }

        public override void Open()
        {
            if (State != ConnectionState.Open)
            {
                base.Open();
            }

            _lasteUsedTime = DateTime.Now;
        }

        /// <summary> 是否存活 </summary>
        /// <returns></returns>
        public bool IsAlive => _refCount > 0 || (DateTime.Now - _lasteUsedTime) < _aliveTime;

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <param name="disposing">确认释放</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (_isTransaction ? Interlocked.Decrement(ref _refCount) == 0 : (DateTime.Now - _lasteUsedTime) >= _aliveTime))
            {
                base.Dispose(true);

                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// 关闭链接
        /// </summary>
        public override void Close() { }
    }
}

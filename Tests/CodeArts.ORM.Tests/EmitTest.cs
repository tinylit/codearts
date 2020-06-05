using System;
using CodeArts.DbAnnotations;
using CodeArts.SqlServer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;
using System.Linq;
using System.Collections.Concurrent;
using System.Timers;
using System.Data;

namespace CodeArts.ORM.Tests
{
    [SqlServerConnection]
    [TypeGen(typeof(DbTypeGen))]
    public interface IUser : IDbMapper<FeiUsers>
    {
        [Select("SELECT * FROM fei_users WHERE uid>={id} AND uid<{max_id}")]
        List<FeiUsers> GetUser(int id, int max_id);

        [Select("SELECT * FROM fei_users WHERE uid={id}")]
        FeiUsers GetUser(int id);
    }

    [TestClass]
    public class EmitTest
    {

        /// <summary>
        /// 默认链接
        /// </summary>
        private class DefaultConnections : IDispatchConnections
        {
            private bool _clearTimerRun;
            private readonly Timer _clearTimer;
            private readonly ConcurrentDictionary<string, List<DbConnection>> connectionCache = new ConcurrentDictionary<string, List<DbConnection>>();

            public DefaultConnections()
            {
                _clearTimer = new Timer(1000 * 60);
                _clearTimer.Elapsed += ClearTimerElapsed;
                _clearTimer.Enabled = true;
                _clearTimer.Stop();
                _clearTimerRun = false;
            }

            private void ClearTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
            {

                var list = new List<string>();
                foreach (var kv in connectionCache)
                {
                    kv.Value.RemoveAll(x =>
                    {
                        if (x.IsAlive)
                        {
                            return false;
                        }

                        x.Dispose();

                        return true;
                    });

                    if (kv.Value.Count == 0)
                    {
                        list.Add(kv.Key);
                    }
                }

                list.ForEach(key =>
                {
                    if (connectionCache.TryRemove(key, out List<DbConnection> connections) && connections.Count > 0)
                    {
                        connectionCache.TryAdd(key, connections);
                    }
                });

                if (connectionCache.Count == 0)
                {
                    _clearTimerRun = false;
                    _clearTimer.Stop();
                }
            }
            /// <summary>
            /// 链接管理
            /// </summary>
            /// <param name="connectionString">数据库连接字符串</param>
            /// <param name="adapter">适配器</param>
            /// <param name="useCache">是否使用缓存</param>
            /// <returns></returns>
            public IDbConnection Create(string connectionString, IDbConnectionAdapter adapter, bool useCache = true)
            {
                List<DbConnection> connections = connectionCache.GetOrAdd(connectionString, _ => new List<DbConnection>());

                foreach (var item in connections)
                {
                    if (item.IsAlive && item.IsIdle)
                    {
                        return item;
                    }

                }

                var connection = new DbConnection(adapter.Create(connectionString), adapter.ConnectionHeartbeat);

                connections.Add(connection);

                if (!_clearTimerRun)
                {
                    _clearTimer.Start();
                    _clearTimerRun = true;
                }

                return connection;
            }
        }
        [TestMethod]
        public void MyTestMethod()
        {
            var adapter = new SqlServerAdapter();
            DbConnectionManager.RegisterAdapter(adapter);
            DbConnectionManager.RegisterProvider<CodeArtsProvider>();

            IUser user = (IUser)System.Activator.CreateInstance(new DbTypeGen().Create(typeof(IUser)));

            var userDto = user.GetUser(10, 100).ToList();
        }
    }
}

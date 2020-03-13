using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace UnitTest
{
    [TestClass]
    public class ThreadSecurityTest
    {
        private static readonly object LockObj = new object();
        //连接池（线程及对应的数据库连接）
        private static readonly ConcurrentDictionary<Thread, Dictionary<string, int>> ConnectionCache = new ConcurrentDictionary<Thread, Dictionary<string, int>>();
        [TestMethod]
        public void InForEachTest()
        {
            ConnectionCache.TryAdd(Thread.CurrentThread, new Dictionary<string, int>
            {
                { "test",1}
            });
            var clearConnThread = new List<Thread>();
            Dictionary<string, int> connDict = null;
            var clearConn = new Dictionary<string, int>();
            lock (LockObj)
            {
                //标记
                foreach (var key in ConnectionCache.Keys)
                {
                    if (!ConnectionCache.TryGetValue(key, out connDict))
                        continue;

                    foreach (var kv in connDict)
                    {
                        //添加删除信息
                        clearConn.Add(kv.Key, kv.Value);
                    }
                    if (clearConn.Count == connDict.Count)
                    {
                        clearConnThread.Add(key);
                    }
                    foreach (var kv in clearConn)
                    {
                        connDict.Remove(kv.Key);
                    }
                }
                foreach (var item in clearConnThread)
                {
                    ConnectionCache.TryRemove(item, out _);
                }
            }
        }
    }
}

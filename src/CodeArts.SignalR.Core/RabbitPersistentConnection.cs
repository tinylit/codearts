#if NET45 || NET451 || NET452 || NET461
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.SignalR.Core
{
    /// <summary>
    /// Rabbit 做持久链接
    /// </summary>
    public class RabbitPersistentConnection : PersistentConnection
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public RabbitPersistentConnection()
        {
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="request">请求</param>
        /// <param name="connectionId">连接ID</param>
        /// <returns></returns>
        protected override Task OnConnected(IRequest request, string connectionId)
        {
            return base.OnConnected(request, connectionId);
        }
        /// <summary>
        /// 重连
        /// </summary>
        /// <param name="request">请求</param>
        /// <param name="connectionId">连接ID</param>
        /// <returns></returns>
        protected override Task OnReconnected(IRequest request, string connectionId)
        {
            return base.OnReconnected(request, connectionId);
        }

        /// <summary>
        /// 失联
        /// </summary>
        /// <param name="request">请求</param>
        /// <param name="connectionId">连接ID</param>
        /// <param name="stopCalled"></param>
        /// <returns></returns>
        protected override Task OnDisconnected(IRequest request, string connectionId, bool stopCalled)
        {
            return base.OnDisconnected(request, connectionId, stopCalled);
        }
    }
}
#endif
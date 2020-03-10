#if NET45 || NET451 || NET452 ||NET461
using CodeArts.SignalR.Messaging;
using System;
using System.Threading.Tasks;

namespace CodeArts.SignalR
{
    /// <summary>
    /// 信箱
    /// </summary>
    public interface IMail
    {
        /// <summary>
        /// 收件
        /// </summary>
        /// <param name="id">收件人ID</param>
        /// <param name="message">信息</param>
        /// <returns></returns>
        Task Accept(string id, Message message);

        /// <summary>
        /// 发件
        /// </summary>
        /// <param name="id">收件人ID</param>
        /// <param name="connect">交接</param>
        void Send(string id, Func<Message, Task> connect);
    }
}
#endif
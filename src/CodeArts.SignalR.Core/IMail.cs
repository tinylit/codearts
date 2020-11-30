#if NET45 || NET451 || NET452 ||NET461
using CodeArts.SignalR.Messaging;
using System;
using System.Threading.Tasks;

namespace CodeArts.SignalR
{
    /// <summary>
    /// 信箱。
    /// </summary>
    public interface IMail
    {
        /// <summary>
        /// 收件。
        /// </summary>
        /// <param name="message">信息。</param>
        /// <returns></returns>
        Task All(Message message);

        /// <summary>
        /// 收件。
        /// </summary>
        /// <param name="group">分组。</param>
        /// <param name="message">信息。</param>
        /// <returns></returns>
        Task Group(string group, Message message);

        /// <summary>
        /// 收件。
        /// </summary>
        /// <param name="userId">用户。</param>
        /// <param name="message">信息。</param>
        /// <returns></returns>
        Task User(string userId, Message message);

        /// <summary>
        /// 收件。
        /// </summary>
        /// <param name="connectionId">连接。</param>
        /// <param name="message">信息。</param>
        /// <returns></returns>
        Task Client(string connectionId, Message message);

        /// <summary>
        /// 发件。
        /// </summary>
        /// <param name="userId">收件人ID。</param>
        /// <param name="hub">消息中心。</param>
        /// <param name="connect">交接。</param>
        void SendToUser(string userId, string hub, Func<Message, Task> connect);

        /// <summary>
        /// 发件。
        /// </summary>
        /// <param name="connectionId">收件人ID。</param>
        /// <param name="hub">消息中心。</param>
        /// <param name="connect">交接。</param>
        void SendToConnection(string connectionId, string hub, Func<Message, Task> connect);
    }
}
#endif
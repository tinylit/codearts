using CodeArts.Caching;
using CodeArts.SignalR.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeArts.SignalR
{
    /// <summary>
    /// 默认信箱。
    /// </summary>
    public class DefaultMail : IMail
    {
        private readonly double expires;
        private readonly ICaching cache;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="expires">数据存放时间，以最后一次收件时间为准（单位：分钟）。</param>
        public DefaultMail(double expires = 1440D) : this(CachingManager.GetCache("codearts-signalr-mail"), expires)
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="cache">缓存器。</param>
        /// <param name="expires">数据存放时间，以最后一次收件时间为准（单位：分钟）。</param>
        public DefaultMail(ICaching cache, double expires = 1440D)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));

            if (expires <= 0D)
            {
                throw new IndexOutOfRangeException(nameof(expires));
            }

            this.expires = expires;
        }

        private Task AsTask(string key, Message message)
        {
            return Task.Factory.StartNew(() =>
            {
                var list = cache.Get<List<Message>>(key) ?? new List<Message>();

                list.Add(message);

                cache.Set(key, list, TimeSpan.FromMinutes(expires));
            });
        }

        private Task Empty()
        {
#if NET461
            return Task.CompletedTask;
#else
            return Task.Factory.StartNew(() => { });
#endif
        }

        private void Send(string key, Func<Message, Task> connect)
        {
            var list = cache.Get<List<Message>>(key);

            if (list is null || list.Count == 0)
            {
                return;
            }

            cache.Remove(key);

            list.ForEach(async message =>
            {
                await connect.Invoke(message).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// 收件。
        /// </summary>
        /// <param name="message">信息。</param>
        /// <returns></returns>
        public Task All(Message message) => Empty();

        /// <summary>
        /// 收件。
        /// </summary>
        /// <param name="connectionId">连接。</param>
        /// <param name="message">信息。</param>
        /// <returns></returns>
        public Task Client(string connectionId, Message message) => AsTask($"hc-{message.Hub}.{connectionId}", message);

        /// <summary>
        /// 收件。
        /// </summary>
        /// <param name="group">分组。</param>
        /// <param name="message">信息。</param>
        /// <returns></returns>
        public Task Group(string group, Message message) => Empty();

        /// <summary>
        /// 收件。
        /// </summary>
        /// <param name="userId">用户。</param>
        /// <param name="message">信息。</param>
        /// <returns></returns>
        public Task User(string userId, Message message) => AsTask($"hu-{message.Hub}.{userId}", message);

        /// <summary>
        /// 发件。
        /// </summary>
        /// <param name="userId">收件人ID。</param>
        /// <param name="hub">消息中心。</param>
        /// <param name="connect">交接。</param>
        public void SendToUser(string userId, string hub, Func<Message, Task> connect) => Send($"hu-{hub}.{userId}", connect);

        /// <summary>
        /// 发件。
        /// </summary>
        /// <param name="connectionId">收件人ID。</param>
        /// <param name="hub">消息中心。</param>
        /// <param name="connect">交接。</param>
        public void SendToConnection(string connectionId, string hub, Func<Message, Task> connect) => Send($"hc-{hub}.{connectionId}", connect);
    }
}

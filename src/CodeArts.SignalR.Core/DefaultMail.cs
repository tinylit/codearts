using CodeArts.Cache;
using CodeArts.SignalR.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeArts.SignalR
{
    /// <summary>
    /// 默认信箱
    /// </summary>
    public class DefaultMail : IMail
    {
        private readonly double expires;
        private readonly ICache mailCache;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="expires">数据存放时间，以最后一次收件时间为准（单位：分钟）</param>
        public DefaultMail(double expires = 1440D) : this(CacheManager.GetCache("codearts-signalr-mail"), expires)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cache">缓存器</param>
        /// <param name="expires">数据存放时间，以最后一次收件时间为准（单位：分钟）</param>
        public DefaultMail(ICache cache, double expires = 1440D)
        {
            this.expires = expires;
            mailCache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// 收件
        /// </summary>
        /// <param name="id">收件人ID</param>
        /// <param name="message">信息</param>
        /// <returns></returns>
        public Task Accept(string id, Message message)
        {
            return Task.Factory.StartNew(() =>
            {
                var list = mailCache.Get<List<Message>>(id) ?? new List<Message>();

                list.Add(message);

                mailCache.Set(id, list, TimeSpan.FromMinutes(expires));
            });
        }

        /// <summary>
        /// 发件
        /// </summary>
        /// <param name="id">收件人ID</param>
        /// <param name="connect">交接</param>
        public void Send(string id, Func<Message, Task> connect)
        {
            var list = mailCache.Get<List<Message>>(id);

            if (list is null || list.Count == 0)
            {
                return;
            }

            list.ForEach(async message =>
            {
                await connect.Invoke(message);
            });

            mailCache.Remove(id);
        }
    }
}

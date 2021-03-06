﻿#if NET45 || NET451 || NET452 ||NET461
using CodeArts.Serialize.Json;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using System.Threading.Tasks;

namespace CodeArts.SignalR.Messaging
{
    /// <summary>
    /// 信箱运输通道。
    /// </summary>
    public class MessageMailBus : MessageBus
    {
        private readonly IMail mail;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="resolver">解决器。</param>
        /// <param name="mail">信箱。</param>
        public MessageMailBus(IDependencyResolver resolver, IMail mail) : base(resolver)
        {
            this.mail = mail ?? throw new System.ArgumentNullException(nameof(mail));
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="stringMinifier">内容压缩。</param>
        /// <param name="traceManager">日志。</param>
        /// <param name="performanceCounterManager">性能。</param>
        /// <param name="configurationManager">配置。</param>
        /// <param name="maxTopicsWithNoSubscriptions">最大订阅数。</param>
        public MessageMailBus(IStringMinifier stringMinifier, ITraceManager traceManager, IPerformanceCounterManager performanceCounterManager, IConfigurationManager configurationManager, int maxTopicsWithNoSubscriptions) : base(stringMinifier, traceManager, performanceCounterManager, configurationManager, maxTopicsWithNoSubscriptions)
        {
        }
        /// <summary>
        /// 推送消息。
        /// </summary>
        /// <param name="message">消息。</param>
        /// <returns></returns>
        public override Task Publish(Microsoft.AspNet.SignalR.Messaging.Message message)
        {
            string key = message.Key;

            if (message.IsCommand || message.Filter?.Length > 0 || Topics.TryGetValue(key, out Topic topic) && topic.Subscriptions.Count > 0)
            {
                return base.Publish(message);
            }

            Message msg = JsonHelper.Json<Message>(message.GetString());

            if (key.StartsWith("h-"))
            {
                return mail.All(msg);
            }

            int indexOfDot = key.IndexOf('.');

            string name = key.Substring(indexOfDot + 1);

            if (key.StartsWith("hu-"))
            {
                return mail.User(name, msg);
            }

            if (key.StartsWith("hc-"))
            {
                return mail.Client(name, msg);
            }

            if (key.StartsWith("hg-"))
            {
                return mail.Group(name, msg);
            }

            return base.Publish(message);
        }
    }
}
#endif
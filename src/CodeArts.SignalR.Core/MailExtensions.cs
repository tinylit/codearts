#if NET45 || NET451 || NET452 ||NET461
using CodeArts.SignalR;
using CodeArts.SignalR.Messaging;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR.Messaging;
using System;

namespace Owin
{
    /// <summary>
    /// Mail 扩展
    /// </summary>
    public static class MailExtensions
    {
        /// <summary>
        /// 使用信箱功能
        /// </summary>
        /// <param name="app">项目构建器</param>
        /// <param name="mail">信箱</param>
        /// <returns></returns>
        public static void RunSignalR(this IAppBuilder app, IMail mail) => app.RunSignalR(new HubConfiguration(), mail);

        /// <summary>
        /// 使用信箱功能
        /// </summary>
        /// <param name="app">项目构建器</param>
        /// <param name="configuration">配置</param>
        /// <param name="mail">信箱</param>
        /// <returns></returns>
        public static void RunSignalR(this IAppBuilder app, HubConfiguration configuration, IMail mail)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (mail is null)
            {
                throw new ArgumentNullException(nameof(mail));
            }

            var bus = new Lazy<MessageMailBus>(() => new MessageMailBus(configuration.Resolver, mail));
            configuration.Resolver.Register(typeof(IMessageBus), () => bus.Value);

            var hubPipeline = configuration.Resolver.Resolve<IHubPipeline>();
            hubPipeline.AddModule(new MailPipelineModule(mail));

            app.RunSignalR(configuration);
        }

        /// <summary>
        /// 使用信箱功能
        /// </summary>
        /// <typeparam name="TConnection">持久连接</typeparam>
        /// <param name="app">项目构建器</param>
        /// <param name="mail">信箱</param>
        public static void RunSignalR<TConnection>(this IAppBuilder app, IMail mail) where TConnection : PersistentConnection
            => app.RunSignalR<TConnection>(new ConnectionConfiguration(), mail);

        /// <summary>
        /// 使用信箱功能
        /// </summary>
        /// <typeparam name="TConnection">持久连接</typeparam>
        /// <param name="app">项目构建器</param>
        /// <param name="configuration">连接配置</param>
        /// <param name="mail">信箱</param>
        public static void RunSignalR<TConnection>(this IAppBuilder app, ConnectionConfiguration configuration, IMail mail) where TConnection : PersistentConnection
            => app.RunSignalR(typeof(TConnection), configuration, mail);

        /// <summary>
        /// 使用信箱功能
        /// </summary>
        /// <param name="app">项目构建器</param>
        /// <param name="connectionType">连接类型</param>
        /// <param name="configuration">连接配置</param>
        /// <param name="mail">信箱</param>
        public static void RunSignalR(this IAppBuilder app, Type connectionType, ConnectionConfiguration configuration, IMail mail)
        {
            if (connectionType is null)
            {
                throw new ArgumentNullException(nameof(connectionType));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (mail is null)
            {
                throw new ArgumentNullException(nameof(mail));
            }

            var bus = new Lazy<MessageMailBus>(() => new MessageMailBus(configuration.Resolver, mail));
            configuration.Resolver.Register(typeof(IMessageBus), () => bus.Value);

            var hubPipeline = configuration.Resolver.Resolve<IHubPipeline>();
            hubPipeline.AddModule(new MailPipelineModule(mail));

            app.RunSignalR(connectionType, configuration);
        }
    }
}
#endif
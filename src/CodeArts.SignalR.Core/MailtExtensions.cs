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
    /// Owin 扩展
    /// </summary>
    public static class MailtExtensions
    {
        /// <summary>
        /// 使用信箱功能。
        /// </summary>
        /// <param name="app">项目构建器</param>
        /// <param name="mail">信箱</param>
        /// <returns></returns>
        public static void RunSignalR(this IAppBuilder app, IMail mail) => app.RunSignalR(new HubConfiguration(), mail);

        /// <summary>
        /// 使用信箱功能。
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
    }
}
#endif
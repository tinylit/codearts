#if NET45 || NET451 || NET452 ||NET461
using SkyBuilding.SignalR;
using System;

namespace Owin
{
    /// <summary>
    /// Jwt 认证事件
    /// </summary>
    public class JwtBearerEvents
    {
        /// <summary>
        /// 接收消息
        /// </summary>
        public Action<MessageReceivedContext> OnMessageReceived { get; set; }

        /// <summary>
        /// 令牌验证
        /// </summary>
        public Action<TokenValidateContext> OnTokenValidate { get; set; }

        /// <summary>
        /// 令牌已验证
        /// </summary>
        public Action<TokenValidatedContext> OnTokenValidated { get; set; }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="context">上下文</param>
        public virtual void MessageReceived(MessageReceivedContext context) => OnMessageReceived?.Invoke(context);

        /// <summary>
        /// 验证消息
        /// </summary>
        /// <param name="context">上下文</param>
        /// <returns></returns>
        public virtual void TokenValidate(TokenValidateContext context) => OnTokenValidate?.Invoke(context);

        /// <summary>
        /// 已验证消息
        /// </summary>
        /// <param name="context">上下文</param>
        /// <returns></returns>
        public virtual void TokenValidated(TokenValidatedContext context) => OnTokenValidated?.Invoke(context);
    }
}
#endif
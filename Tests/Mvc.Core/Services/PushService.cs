using Grpc.Core;
using System.Threading.Tasks;

namespace Mvc.Core
{
    /// <summary>
    /// 推送服务。
    /// </summary>
    public class PushService : Push.PushBase
    {
        /// <summary>
        /// 妥善。
        /// </summary>
        /// <param name="request">请求信息。</param>
        /// <param name="context">上下文。</param>
        /// <returns></returns>
        public override Task<PushResult> Push(PushRequest request, ServerCallContext context)
        {
            return new Task<PushResult>(() => new PushResult { Code = 200 });
        }
    }
}

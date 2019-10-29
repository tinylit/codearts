using Grpc.Core;
using System.Threading.Tasks;

namespace Mvc.Core
{
    /// <summary>
    /// ���ͷ���
    /// </summary>
    public class PushService : Push.PushBase
    {
        /// <summary>
        /// ����
        /// </summary>
        /// <param name="request">������Ϣ</param>
        /// <param name="context">������</param>
        /// <returns></returns>
        public override Task<PushResult> Push(PushRequest request, ServerCallContext context)
        {
            return base.Push(request, context);
        }
    }
}
#if NET40 || NET_NORMAL
using System.Collections;
using System.Collections.Generic;

namespace CodeArts.Mvc.DependencyInjection
{
    /// <summary>
    /// 服务集合。
    /// </summary>
    public interface IServiceCollection : IList<ServiceDescriptor>, ICollection<ServiceDescriptor>, IEnumerable<ServiceDescriptor>, IEnumerable
    {
    }
}
#endif
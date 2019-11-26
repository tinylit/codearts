#if NET40 || NET45 || NET451 || NET452 || NET461
using System.Collections;
using System.Collections.Generic;

namespace SkyBuilding.Mvc.DependencyInjection
{
    /// <summary>
    /// 服务集合
    /// </summary>
    public interface IServiceCollection : IList<ServiceDescriptor>, ICollection<ServiceDescriptor>, IEnumerable<ServiceDescriptor>, IEnumerable
    {
    }
}
#endif
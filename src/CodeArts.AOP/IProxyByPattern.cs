using Microsoft.Extensions.DependencyInjection;
namespace CodeArts
{
    /// <summary>
    /// 代理方式。
    /// </summary>
    public interface IProxyByPattern
    {
        /// <summary>
        /// 创建类型。
        /// </summary>
        /// <returns></returns>
        ServiceDescriptor Resolve();
    }
}

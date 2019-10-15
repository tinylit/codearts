
namespace SkyBuilding.Cache
{
    /// <summary>
    /// 缓存服务供应商
    /// </summary>
    public interface ICacheProvider
    {
        /// <summary> 获取缓存对象 </summary>
        /// <param name="name">缓存名称</param>
        /// <returns></returns>
        ICache GetCache(string name);
    }
}

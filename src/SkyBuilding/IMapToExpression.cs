using System;

namespace SkyBuilding
{
    /// <summary>
    /// 映射表达式
    /// </summary>
    public interface IMapToExpression : IProfileExpression, IProfileConfiguration, IProfile
    {
        /// <summary>
        /// 拷贝
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">数据源</param>
        /// <returns></returns>
        T MapTo<T>(object source, T def = default);

        /// <summary>
        /// 拷贝
        /// </summary>
        /// <param name="source">数据源</param>
        /// <param name="conversionType">类型</param>
        /// <returns></returns>
        object MapTo(object source, Type conversionType);
    }
}

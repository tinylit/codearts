using System;

namespace SkyBuilding
{
    /// <summary>
    /// 拷贝表达式
    /// </summary>
    public interface ICopyToExpression : IProfileExpression, IProfileConfiguration, IProfile
    {
        /// <summary>
        /// 拷贝
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">数据源</param>
        /// <returns></returns>
        T CopyTo<T>(T source, T def = default);
    }
}

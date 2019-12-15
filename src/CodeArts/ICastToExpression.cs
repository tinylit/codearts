using System;

namespace CodeArts
{
    /// <summary>
    /// 转换配置
    /// </summary>
    public interface ICastToExpression : IProfileExpression, IProfile
    {
        /// <summary>
        /// 转换
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="def">默认值</param>
        /// <returns></returns>
        T CastTo<T>(object source, T def = default);

        /// <summary>
        /// 转换
        /// </summary>
        /// <param name="source">数据源</param>
        /// <param name="conversionType">类型</param>
        /// <returns></returns>
        object CastTo(object source, Type conversionType);
    }
}

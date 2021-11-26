using System;

namespace CodeArts.Casting
{
    /// <summary>
    /// 映射。
    /// </summary>
    public interface IMapExpression
    {
        /// <summary>
        /// 是否匹配。
        /// </summary>
        /// <param name="sourceType">源类型。</param>
        /// <param name="conversionType">目标类型。</param>
        /// <returns></returns>
        bool IsMatch(Type sourceType, Type conversionType);
    }
}

using System;

namespace CodeArts
{
    /// <summary>
    /// 配置文件。
    /// </summary>
    public interface IProfile
    {
        /// <summary>
        /// 创建工厂。
        /// </summary>
        /// <param name="sourceType">源数据类型。</param>
        /// <typeparam name="T">目标数据类型。</typeparam>
        /// <returns></returns>
        Func<object, T> Create<T>(Type sourceType);
    }
}

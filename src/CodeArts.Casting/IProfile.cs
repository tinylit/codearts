using System;

namespace CodeArts.Casting
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
        /// <typeparam name="TResult">目标数据类型。</typeparam>
        /// <returns></returns>
        Func<object, TResult> CreateMap<TResult>(Type sourceType);
    }
}

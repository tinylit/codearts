using System;

namespace CodeArts.Config
{
    /// <summary>
    /// 配置文件帮助类
    /// </summary>
    public interface IConfigHelper
    {
        ///<summary> 配置文件变更事件 </summary>
        event Action<object> OnConfigChanged;

        /// <summary>
        /// 配置文件读取
        /// </summary>
        /// <typeparam name="T">读取数据类型</typeparam>
        /// <param name="key">健</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        T Get<T>(string key, T defaultValue = default);
    }
}

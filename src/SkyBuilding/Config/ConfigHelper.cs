using System;

namespace SkyBuilding.Config
{
    /// <summary>
    /// 配置文件帮助类
    /// </summary>
    public class ConfigHelper : DesignMode.Singleton<ConfigHelper>
    {
        private readonly IConfigHelper _helper;
        /// <summary>
        /// 构造函数
        /// </summary>
        private ConfigHelper() => _helper = RuntimeServManager.Singleton<IConfigHelper>();

        ///<summary> 配置文件变更事件 </summary>
        public event Action<object> OnConfigChanged
        {
            add
            {
                _helper.OnConfigChanged += value;
            }
            remove
            {
                _helper.OnConfigChanged -= value;
            }
        }

        /// <summary>
        /// 配置文件读取
        /// </summary>
        /// <typeparam name="T">读取数据类型</typeparam>
        /// <param name="key">健</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public T Get<T>(string key, T defaultValue = default) => _helper.Get(key, defaultValue);
    }
}

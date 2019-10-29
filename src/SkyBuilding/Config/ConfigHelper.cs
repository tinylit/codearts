using System;

namespace SkyBuilding.Config
{
    /// <summary>
    /// 配置文件帮助类
    /// </summary>
    public class ConfigHelper : DesignMode.Singleton<ConfigHelper>
    {
        /// <summary>
        /// 默认配置助手
        /// </summary>
        private class DefaultConfigHelper : IConfigHelper
        {
            /// <summary>
            /// 配置改变事件
            /// </summary>
            public event Action<object> OnConfigChanged { add { } remove { } }

            /// <summary>
            /// 默认值
            /// </summary>
            /// <typeparam name="T">返回数据类型</typeparam>
            /// <param name="key">键</param>
            /// <param name="defaultValue">默认值</param>
            /// <returns></returns>
            public T Get<T>(string key, T defaultValue = default) => defaultValue;
        }

        private IConfigHelper _helper;

        /// <summary>
        /// 构造函数
        /// </summary>
        private ConfigHelper() => _helper = RuntimeServManager.Singleton<IConfigHelper, DefaultConfigHelper>(helper => _helper = helper);

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

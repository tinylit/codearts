using System;

namespace CodeArts.Config
{
    /// <summary>
    /// 配置文件帮助类。
    /// </summary>
    public class ConfigHelper : Singleton<ConfigHelper>
    {
        /// <summary>
        /// 默认配置助手。
        /// </summary>
        private class DefaultConfigHelper : IConfigHelper
        {
            /// <summary>
            /// 配置改变事件。
            /// </summary>
            public event Action<object> OnConfigChanged { add { } remove { } }

            /// <summary>
            /// 默认值。
            /// </summary>
            /// <typeparam name="T">返回数据类型。</typeparam>
            /// <param name="key">键。</param>
            /// <param name="defaultValue">默认值。</param>
            /// <returns></returns>
            public T Get<T>(string key, T defaultValue = default) => defaultValue;
        }

        private readonly IConfigHelper _helper;

        private ConfigHelper() => _helper = RuntimeServPools.Singleton<IConfigHelper, DefaultConfigHelper>();

        ///<summary> 配置文件变更事件。</summary>
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
        /// 配置文件读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="key">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        public T Get<T>(string key, T defaultValue = default) => _helper.Get(key, defaultValue);

        /// <summary>
        /// 配置文件读取（数据变更时，同时更新结果）。
        /// </summary>
        /// <typeparam name="TConfigable">配置能力。</typeparam>
        /// <param name="key">健。</param>
        /// <returns></returns>
        public TConfigable Get<TConfigable>(string key) where TConfigable : class, IConfigable<TConfigable>
        {
            var value = _helper.Get(key, default(TConfigable));

            if (value is null)
            {
                return default;
            }

            _helper.OnConfigChanged += _ => value.SaveChanges(_helper.Get(key, default(TConfigable)));

            return value;
        }
    }
}

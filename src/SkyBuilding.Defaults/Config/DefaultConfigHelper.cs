#if NETSTANDARD2_0 ||NETSTANDARD2_1
using Microsoft.Extensions.Configuration;
using SkyBuilding.Config;
using System;
using System.IO;
using System.Linq;

namespace SkyBuilding.Config
{
    /// <summary>
    /// Json 配置助手
    /// </summary>
    public class DefaultConfigHelper : DesignMode.Singleton<DefaultConfigHelper>, IConfigHelper
    {
        /// <summary>
        /// 单例构造
        /// </summary>
        private DefaultConfigHelper()
        {
            InitBuilder();
            InitConfig();
        }

        //系统盘
        private const string SystemDevice = "C:\\";

        private IConfigurationRoot _config;
        private IDisposable _callbackRegistration;
        private IConfigurationBuilder _builder;

        /// <summary> 配置文件变更事件 </summary>
        public event Action<object> OnConfigChanged;

        /// <summary> 当前配置 </summary>
        public IConfiguration Config => _config;

        /// <summary>
        /// 初始化构造器
        /// </summary>
        /// <param name="useConfigCenter"></param>
        private void InitBuilder(bool useConfigCenter = false)
        {
            string currentDir = useConfigCenter ?
                SystemDevice :
                Directory.GetCurrentDirectory();

            _builder = new ConfigurationBuilder()
                .SetBasePath(currentDir);

            var path = Path.Combine(currentDir, "appsettings.json");

            if (File.Exists(path))
            {
                _builder.AddJsonFile(path, false, true);
            }
        }

        /// <summary>
        /// 初始化配置
        /// </summary>
        private void InitConfig()
        {
            _config = _builder.Build();
            _callbackRegistration = _config.GetReloadToken()
                .RegisterChangeCallback(ConfigChanged, _config);
        }

        /// <summary>
        /// 配置变更事件
        /// </summary>
        /// <param name="state"></param>
        private void ConfigChanged(object state)
        {
            OnConfigChanged?.Invoke(state);
            _callbackRegistration?.Dispose();
            _callbackRegistration = _config.GetReloadToken()
                .RegisterChangeCallback(ConfigChanged, state);
        }

        /// <summary> 构建额外配置 </summary>
        /// <param name="builderAction"></param>
        public void Build(Action<IConfigurationBuilder> builderAction)
        {
            builderAction.Invoke(_builder);
            var sources = _builder.Sources.Reverse().ToArray();
            //倒序排列，解决读取配置时的优先级问题
            for (var i = 0; i < sources.Length; i++)
            {
                _builder.Sources[i] = sources[i];
            }

            _config = _builder.Build();
        }

        /// <summary>
        /// 配置文件读取
        /// </summary>
        /// <typeparam name="T">读取数据类型</typeparam>
        /// <param name="key">健</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            //简单类型直接获取其值
            try
            {
                var type = typeof(T);
                if (type.IsSimpleType())
                    return _config.GetValue(key, defaultValue);

                //其他复杂类型
                return _config.GetSection(key).Get<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary> 重新加载配置 </summary>
        public void Reload() { _config.Reload(); }
    }
}
#else
using SkyBuilding.Config;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web.Configuration;

namespace SkyBuilding.Config
{
    /// <summary>
    /// Json 配置助手
    /// </summary>
    public class DefaultConfigHelper : DesignMode.Singleton<DefaultConfigHelper>, IConfigHelper
    {
        private readonly Configuration Config;
        private readonly Dictionary<string, string> Configs;

        private DefaultConfigHelper()
        {
            Configs = new Dictionary<string, string>();

            Config = WebConfigurationManager.OpenWebConfiguration("~");

            Reload();

            var filePath = Config.FilePath;
            var fileName = Path.GetFileName(filePath);
            var path = filePath.Substring(0, filePath.Length - fileName.Length);

            using (var watcher = new FileSystemWatcher(path, fileName))
            {
                watcher.Changed += Watcher_Changed;
            }
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            OnConfigChanged?.Invoke(sender);

            Reload();
        }

        /// <summary>
        /// 无效
        /// </summary>
        public event Action<object> OnConfigChanged;

        public T Get<T>(string key, T defaultValue = default)
        {
            if (Configs.TryGetValue(key, out string value))
            {
                return value.CastTo(defaultValue);
            }

            return defaultValue;
        }

        /// <summary> 重新加载配置 </summary>
        public void Reload()
        {
            Configs.Clear();

            var appSettings = Config.AppSettings;

            foreach (KeyValueConfigurationElement kv in appSettings.Settings)
            {
                Configs.Add(kv.Key, kv.Value);
            }
        }
    }
}
#endif
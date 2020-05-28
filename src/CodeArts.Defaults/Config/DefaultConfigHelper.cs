#if NETSTANDARD2_0 ||NETSTANDARD2_1
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;

namespace CodeArts.Config
{
    /// <summary>
    /// Json 配置助手
    /// </summary>
    public class DefaultConfigHelper : IConfigHelper
    {
        private static IConfigurationBuilder _builder;

        /// <summary>
        /// 获取默认配置
        /// </summary>
        /// <returns></returns>
        static IConfigurationBuilder ConfigurationBuilder()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string variable = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.Equals(variable, "Development", StringComparison.OrdinalIgnoreCase))
            {
                string dir = Directory.GetCurrentDirectory();

                if (File.Exists(Path.Combine(dir, "appsettings.json")))
                {
                    baseDir = dir;
                }

            }

            var builder = new ConfigurationBuilder()
                 .SetBasePath(baseDir);

            var path = Path.Combine(baseDir, "appsettings.json");

            if (File.Exists(path))
            {
                builder.AddJsonFile(path, false, true);
            }

            return builder;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public DefaultConfigHelper() : this(_builder ?? (_builder = ConfigurationBuilder()))
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="builder">配置</param>
        public DefaultConfigHelper(IConfigurationBuilder builder) : this(builder.Build())
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">配置</param>
        public DefaultConfigHelper(IConfigurationRoot config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _callbackRegistration = config.GetReloadToken()
                .RegisterChangeCallback(ConfigChanged, config);
        }

        private readonly IConfigurationRoot _config;
        private IDisposable _callbackRegistration;

        /// <summary> 配置文件变更事件 </summary>
        public event Action<object> OnConfigChanged;

        /// <summary> 当前配置 </summary>
        public IConfiguration Config => _config;

        /// <summary>
        /// 配置变更事件
        /// </summary>
        /// <param name="state">状态</param>
        private void ConfigChanged(object state)
        {
            OnConfigChanged?.Invoke(state);
            _callbackRegistration?.Dispose();
            _callbackRegistration = _config.GetReloadToken()
                .RegisterChangeCallback(ConfigChanged, state);
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
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web.Configuration;

namespace CodeArts.Config
{
    /// <summary>
    /// 运行环境
    /// </summary>
    public enum RuntimeEnvironment
    {
        /// <summary>
        /// Web
        /// </summary>
        Web = 1,
        /// <summary>
        /// From
        /// </summary>
        Form = 2,
        /// <summary>
        /// Service
        /// </summary>
        Service = 3
    }

    /// <summary>
    /// Json 配置助手
    /// </summary>
    public class DefaultConfigHelper : IConfigHelper
    {
        private FileSystemWatcher _watcher;
        private readonly Configuration Config;
        private readonly Dictionary<string, string> Configs;
        private readonly Dictionary<string, ConnectionStringSettings> ConnectionStrings;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DefaultConfigHelper() : this(RuntimeEnvironment.Web) { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="environment">运行环境</param>
        public DefaultConfigHelper(RuntimeEnvironment environment)
        {
            Configs = new Dictionary<string, string>();

            ConnectionStrings = new Dictionary<string, ConnectionStringSettings>();

            switch (environment)
            {
                case RuntimeEnvironment.Form:
                case RuntimeEnvironment.Service:
                    Config = ConfigurationManager.OpenExeConfiguration(string.Empty);
                    break;
                case RuntimeEnvironment.Web:
                default:
                    Config = WebConfigurationManager.OpenWebConfiguration("~");
                    break;
            }

            Reload();
        }

        /// <summary>
        /// 文件内容变动事件
        /// </summary>
        /// <param name="sender">对象实例</param>
        /// <param name="e">事件参数</param>
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            _watcher.Dispose();

            OnConfigChanged?.Invoke(sender);

            Reload();
        }

        /// <summary>
        /// 配置文件改变事件
        /// </summary>
        public event Action<object> OnConfigChanged;

        /// <summary>
        /// 获取配置
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (key.IndexOf('/') == -1)
            {
                if (Configs.TryGetValue(key, out string value))
                {
                    return value.CastTo(defaultValue);
                }

                return defaultValue;
            }

            var keys = key.Split('/');

            if (string.Equals(keys[0], "connectionStrings", StringComparison.OrdinalIgnoreCase))
            {
                if (keys.Length == 1)
                    return ConnectionStrings.MapTo(defaultValue);

                if (ConnectionStrings.TryGetValue(keys[1], out ConnectionStringSettings value))
                {
                    if (keys.Length == 2)
                        return value.MapTo(defaultValue);

                    if (keys.Length > 3) 
                        return defaultValue;

                    if (string.Equals(keys[2], "connectionString", StringComparison.OrdinalIgnoreCase))
                        return value.ConnectionString.CastTo(defaultValue);

                    if (string.Equals(keys[2], "name", StringComparison.OrdinalIgnoreCase))
                        return value.Name.CastTo(defaultValue);

                    if (string.Equals(keys[2], "providerName", StringComparison.OrdinalIgnoreCase))
                        return value.ProviderName.CastTo(defaultValue);
                }

                return defaultValue;
            }

            if (string.Equals(keys[0], "appStrings", StringComparison.OrdinalIgnoreCase))
            {
                if (keys.Length == 1)
                    return Configs.MapTo(defaultValue);

                if (keys.Length == 2 && Configs.TryGetValue(keys[1], out string value))
                    return value.CastTo(defaultValue);

                return defaultValue;
            }

            try
            {
                var sectionGroup = Config.GetSectionGroup(keys[0]);

                if (sectionGroup is null)
                {
                    var section = Config.GetSection(key);

                    if (section is null)
                        return defaultValue;

                    return (T)(object)section;
                }

                var index = 1;

                while (keys.Length > index)
                {
                    bool flag = false;
                    foreach (ConfigurationSectionGroup sectionGroupItem in sectionGroup.SectionGroups)
                    {
                        if (string.Equals(sectionGroupItem.SectionGroupName, keys[index], StringComparison.OrdinalIgnoreCase))
                        {
                            index += 1;
                            flag = true;
                            sectionGroup = sectionGroupItem;
                            break;
                        }
                    }

                    if (!flag) break;
                }

                if (sectionGroup is null)
                    return defaultValue;

                if (keys.Length == index)
                {
                    if (sectionGroup is T sectionValue)
                    {
                        return sectionValue;
                    }

                    return (T)(object)sectionGroup;
                }

                if (keys.Length == (index + 1))
                {
                    foreach (ConfigurationSection section in sectionGroup.Sections)
                    {
                        if (string.Equals(section.SectionInformation.SectionName, keys[index], StringComparison.OrdinalIgnoreCase))
                        {
                            return (T)(object)section;
                        }
                    }
                }

            }
            catch { }

            return defaultValue;
        }

        /// <summary> 重新加载配置 </summary>
        public void Reload()
        {
            ConnectionStrings.Clear();

            var connectionStrings = Config.ConnectionStrings;

            foreach (ConnectionStringSettings stringSettings in connectionStrings.ConnectionStrings)
            {
                ConnectionStrings.Add(stringSettings.Name, stringSettings);
            }

            Configs.Clear();

            var appSettings = Config.AppSettings;

            foreach (KeyValueConfigurationElement kv in appSettings.Settings)
            {
                Configs.Add(kv.Key, kv.Value);
            }

            var filePath = Config.FilePath;
            var fileName = Path.GetFileName(filePath);
            var path = filePath.Substring(0, filePath.Length - fileName.Length);

            _watcher = new FileSystemWatcher(path, fileName)
            {
                EnableRaisingEvents = true
            };

            _watcher.Changed += Watcher_Changed;
        }
    }
}
#endif
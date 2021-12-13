#if NETSTANDARD2_0_OR_GREATER
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Collections.Concurrent;

namespace CodeArts.Config
{
    /// <summary>
    /// Json 配置助手。
    /// </summary>
    public class DefaultConfigHelper : IConfigHelper
    {
        /// <summary>
        /// 获取默认配置。
        /// </summary>
        /// <returns></returns>
        private static IConfigurationBuilder ConfigurationBuilder()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string variable = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            bool isDevelopment = string.Equals(variable, "Development", StringComparison.OrdinalIgnoreCase);

            if (isDevelopment)
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

            if (isDevelopment)
            {
                var pathDev = Path.Combine(baseDir, "appsettings.Development.json");

                if (File.Exists(pathDev))
                {
                    builder.AddJsonFile(pathDev, true, true);
                }
            }

            return builder;
        }

        private static IConfigurationBuilder MakeConfigurationBuilder(string[] configPaths)
        {
            var builder = ConfigurationBuilder();

            if (configPaths is null || configPaths.Length == 0)
            {
                return builder;
            }

            string dir = Directory.GetCurrentDirectory();

            foreach (var path in configPaths)
            {
                if (File.Exists(path))
                {
                    builder.AddJsonFile(path, false, true);

                    continue;
                }

                string absolutePath = Path.Combine(dir, path);

                if (File.Exists(absolutePath))
                {
                    builder.AddJsonFile(absolutePath, false, true);

                    continue;
                }

                throw new FileNotFoundException($"文件“{path}”未找到!");
            }

            return builder;
        }

        private readonly ConcurrentDictionary<string, IConfiguration> _cache = new ConcurrentDictionary<string, IConfiguration>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DefaultConfigHelper() : this(ConfigurationBuilder())
        {

        }

        /// <summary>
        /// 构造函数（除默认文件，添加额外的配置文件）。
        /// </summary>
        /// <param name="configPaths">配置地址。</param>
        public DefaultConfigHelper(params string[] configPaths) : this(MakeConfigurationBuilder(configPaths))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="builder">配置。</param>
        public DefaultConfigHelper(IConfigurationBuilder builder) : this(builder.Build())
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="config">配置。</param>
        public DefaultConfigHelper(IConfigurationRoot config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _callbackRegistration = config.GetReloadToken()
                .RegisterChangeCallback(ConfigChanged, config);
        }

        private readonly IConfigurationRoot _config;
        private IDisposable _callbackRegistration;

        /// <summary> 配置文件变更事件。 </summary>
        public event Action<object> OnConfigChanged;

        /// <summary> 当前配置。 </summary>
        public IConfiguration Config => _config;

        /// <summary>
        /// 配置变更事件。
        /// </summary>
        /// <param name="state">状态。</param>
        private void ConfigChanged(object state)
        {
            _cache.Clear();

            OnConfigChanged?.Invoke(state);
            _callbackRegistration?.Dispose();

            _callbackRegistration = _config.GetReloadToken()
                .RegisterChangeCallback(ConfigChanged, state);
        }

        /// <summary>
        /// 配置文件读取。
        /// </summary>
        /// <typeparam name="T">读取数据类型。</typeparam>
        /// <param name="key">健。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            try
            {
                var type = typeof(T);

                //简单类型直接获取其值
                if (type.IsSimpleType())
                {
                    return _config.GetValue(key, defaultValue);
                }

                // 复杂类型
                return _cache.GetOrAdd(key, name => _config.GetSection(name)).Get<T>();
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary> 重新加载配置。 </summary>
        public void Reload()
        {
            _cache.Clear();

            _config.Reload();
        }
    }
}
#else
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Web.Configuration;

namespace CodeArts.Config
{
    /// <summary>
    /// 运行环境。
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
    /// Json 配置助手。
    /// </summary>
    public class DefaultConfigHelper : IConfigHelper
    {
        private FileSystemWatcher _watcher;
        private readonly Configuration Config;
        private readonly Dictionary<string, string> Configs;
        private readonly Dictionary<string, ConnectionStringSettings> ConnectionStrings;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public DefaultConfigHelper() : this(RuntimeEnvironment.Web) { }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="environment">运行环境。</param>
        public DefaultConfigHelper(RuntimeEnvironment environment)
        {
            Configs = new Dictionary<string, string>();

            ConnectionStrings = new Dictionary<string, ConnectionStringSettings>(StringComparer.OrdinalIgnoreCase);

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
        /// 文件内容变动事件。
        /// </summary>
        /// <param name="sender">对象实例。</param>
        /// <param name="e">事件参数。</param>
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            _watcher.Dispose();

            OnConfigChanged?.Invoke(sender);

            Reload();
        }

        /// <summary>
        /// 配置文件改变事件。
        /// </summary>
        public event Action<object> OnConfigChanged;

        /// <summary>
        /// 获取配置。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="key">键。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <returns></returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (key.IndexOf('/') == -1)
            {
                if (Configs.TryGetValue(key, out string value))
                {
                    return (T)ChangeType(value, typeof(T), defaultValue);
                }

                return defaultValue;
            }

            var keys = key.Split('/');

            if (string.Equals(keys[0], "connectionStrings", StringComparison.OrdinalIgnoreCase))
            {
                if (keys.Length == 1)
                {
                    return Mapper.Map(ConnectionStrings, defaultValue);
                }

                if (ConnectionStrings.TryGetValue(keys[1], out ConnectionStringSettings value))
                {
                    if (keys.Length == 2)
                    {
                        return Mapper.Map(value, defaultValue);
                    }

                    if (keys.Length > 3)
                    {
                        return defaultValue;
                    }

                    if (string.Equals(keys[2], "connectionString", StringComparison.OrdinalIgnoreCase))
                        return (T)ChangeType(value.ConnectionString, typeof(T), defaultValue);

                    if (string.Equals(keys[2], "name", StringComparison.OrdinalIgnoreCase))
                        return (T)ChangeType(value.Name, typeof(T), defaultValue);

                    if (string.Equals(keys[2], "providerName", StringComparison.OrdinalIgnoreCase))
                        return (T)ChangeType(value.ProviderName, typeof(T), defaultValue);
                }

                return defaultValue;
            }

            if (string.Equals(keys[0], "appStrings", StringComparison.OrdinalIgnoreCase))
            {
                if (keys.Length == 1)
                {
                    return Mapper.Map(Configs, defaultValue);
                }

                if (keys.Length == 2 && Configs.TryGetValue(keys[1], out string value))
                {
                    return (T)ChangeType(value, typeof(T), defaultValue);
                }

                return defaultValue;
            }

            try
            {
                var sectionGroup = Config.GetSectionGroup(keys[0]);

                if (sectionGroup is null)
                {
                    var section = Config.GetSection(key);

                    if (section is null)
                    {
                        return defaultValue;
                    }

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
                {
                    return defaultValue;
                }

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

        /// <summary> 重新加载配置。 </summary>
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

        private static object ChangeType(string source, Type conversionType, object defaultValue)
        {
            if (source is null)
                return defaultValue;

            if (typeof(string) == conversionType)
                return source;

            if (conversionType.IsValueType)
            {
                if (conversionType.IsNullable())
                {
                    conversionType = Nullable.GetUnderlyingType(conversionType);
                }

                try
                {
                    return System.Convert.ChangeType(source, conversionType);
                }
                catch { }

                if (conversionType.IsEnum)
                {
#if NET40_OR_GREATER
                    try
                    {
                        return Enum.Parse(conversionType, source, true);
                    }
                    catch
                    {
                        return defaultValue;
                    }
#else
                    if (Enum.TryParse(conversionType, value, true, out object result))
                    {
                        return result;
                    }

                    return defaultValue;
#endif
                }

                if (conversionType == typeof(bool))
                {
                    if (bool.TryParse(source, out bool result))
                    {
                        return result;
                    }
                }

                if (conversionType == typeof(int) ||
                    conversionType == typeof(bool) ||
                    conversionType == typeof(byte))
                {
                    if (int.TryParse(source, out int result))
                    {
                        if (conversionType == typeof(bool))
                            return result > 0;

                        return System.Convert.ChangeType(result, conversionType);
                    }

                    return defaultValue;
                }

                if (conversionType == typeof(uint))
                {
                    if (uint.TryParse(source, out uint result))
                    {
                        return result;
                    }
                }
                else if (conversionType == typeof(long))
                {
                    if (long.TryParse(source, out long result))
                    {
                        return result;
                    }
                }
                else if (conversionType == typeof(ulong))
                {
                    if (ulong.TryParse(source, out ulong result))
                    {
                        return result;
                    }
                }
                else if (conversionType == typeof(Guid))
                {
                    if (Guid.TryParse(source, out Guid guid))
                    {
                        return guid;
                    }
                }
                else if (conversionType == typeof(DateTime))
                {
                    if (DateTime.TryParse(source, out DateTime date))
                    {
                        return date;
                    }
                }
                else if (conversionType == typeof(DateTimeOffset))
                {
                    if (DateTimeOffset.TryParse(source, out DateTimeOffset timeOffset))
                    {
                        return timeOffset;
                    }
                }
            }
            else if (conversionType == typeof(string))
            {
                return source.ToString();
            }

            return defaultValue;
        }
    }
}
#endif
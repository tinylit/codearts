using System;
using System.Linq;

namespace CodeArts.Db
{
    /// <summary>
    /// 只读连接。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class DbReadConfigAttribute : DbConfigAttribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DbReadConfigAttribute() : base()
        {
        }
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="configName"></param>
        public DbReadConfigAttribute(string configName) : base(configName)
        {
        }

        /// <summary>
        /// 获取数据库链接配置。
        /// </summary>
        /// <returns></returns>
        public override ConnectionConfig GetConfig()
        {
            var connectionConfigs = GetConfigs();

            if (connectionConfigs is null)
            {
                return null;
            }

            int length = connectionConfigs.Length;

            if (length == 1)
            {
                return connectionConfigs[0];
            }

            if (length == 0)
            {
                return null;
            }

            Random random = new Random();

            int totalWeight = connectionConfigs.Sum(x => x.Weight);

            if (totalWeight == 0)
            {
                return connectionConfigs[random.Next(length)];
            }

            int offset = 0;

            int standard = random.Next(totalWeight);

            for (int i = 0; i < length; i++)
            {
                var connectionConfig = connectionConfigs[i];

                offset += connectionConfig.Weight;

                if (offset >= standard)
                {
                    return connectionConfig;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取数据库链接配置。
        /// </summary>
        /// <returns></returns>
        protected virtual ConnectionConfig[] GetConfigs()
        {
            var connectionConfig = base.GetConfig();

            if (connectionConfig is null)
            {
                return new ConnectionConfig[0];
            }

            return new ConnectionConfig[1] { connectionConfig };
        }
    }
}

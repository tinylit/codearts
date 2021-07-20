using System.Collections.Generic;

namespace CodeArts.Emit
{
    /// <summary>
    /// 名称提供者。
    /// </summary>
    public class NamingScope : INamingScope
    {
        private readonly Dictionary<string, int> names = new Dictionary<string, int>();

        /// <summary>
        /// 子命名范围。
        /// </summary>
        /// <returns></returns>
        public INamingScope BeginScope() => new NamingScope();

        /// <summary>
        /// 获取范围内唯一的名称。
        /// </summary>
        /// <param name="displayName">显示名称。</param>
        /// <returns></returns>
        public string GetUniqueName(string displayName)
        {
            if (names.TryGetValue(displayName, out int counter))
            {
                names[displayName] = ++counter;

                return displayName + "_" + counter.ToString();
            }

            names.Add(displayName, 0);

            return displayName;
        }
    }
}

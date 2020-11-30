using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;

namespace CodeArts.NPOI.Xls.Exports
{
    /// <summary>
    /// 键值对类型输出。
    /// </summary>
    public sealed class KeyValuePairExportProvider : ExportProvider<Dictionary<string, object>>
    {
        /// <summary>
        /// 私有构造函数。
        /// </summary>
        private KeyValuePairExportProvider() { }

        /// <summary>
        /// 单例。
        /// </summary>
        public static KeyValuePairExportProvider Instance => Singleton<KeyValuePairExportProvider>.Instance;

        /// <summary>
        /// 导出表册方法。
        /// </summary>
        /// <param name="action">工厂。</param>
        /// <param name="value">数据。</param>
        public override void Export(Func<string, ICell> action, Dictionary<string, object> value)
        {
            foreach (var kv in value)
            {
                var cell = action(kv.Key);

                if (cell is null) continue;

                ValueProvider.SetValue(cell, kv.Value);
            }
        }
    }
}

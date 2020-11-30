using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;

namespace CodeArts.NPOI.Xls.Exports
{
    /// <summary>
    /// 字典数组集合。
    /// </summary>
    public sealed class DictionaryListExportProvider : ExportProvider<Dictionary<string, object>, List<Dictionary<string, object>>, Dictionary<string, object>>
    {
        /// <summary>
        /// 私有无参构造函数。
        /// </summary>
        private DictionaryListExportProvider() { }

        /// <summary>
        /// 单例。
        /// </summary>
        public static DictionaryListExportProvider Instance => Singleton<DictionaryListExportProvider>.Instance;

        /// <summary>
        /// 处理子数据。
        /// </summary>
        /// <param name="findCellByName">通过名称查找表格。</param>
        /// <param name="item">数据。</param>
        public override void ExportChild(Func<string, ICell> findCellByName, Dictionary<string, object> item)
        {
            foreach (var kv in item)
            {
                ICell cell = findCellByName(kv.Key);

                if (cell is null) continue;

                ValueProvider.SetValue(cell, kv.Value);
            }
        }
        /// <summary>
        /// 处理主数据。
        /// </summary>
        /// <param name="findCellByName">通过名称查找表格。</param>
        /// <param name="main">数据。</param>
        public override void ExportMain(Func<string, ICell> findCellByName, Dictionary<string, object> main)
        {
            foreach (var kv in main)
            {
                ICell cell = findCellByName(kv.Key);

                if (cell is null) continue;

                ValueProvider.SetValue(cell, kv.Value);
            }
        }
    }
}

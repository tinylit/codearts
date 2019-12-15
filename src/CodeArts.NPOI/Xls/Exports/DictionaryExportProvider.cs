using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;

namespace CodeArts.NPOI.Xls.Exports
{
    /// <summary>
    /// 字典型输出模型
    /// </summary>
    public sealed class DictionaryExportProvider : ExportProvider<Dictionary<string, object>, Dictionary<string, object>>
    {
        /// <summary>
        /// 私有构造函数
        /// </summary>
        private DictionaryExportProvider() { }

        /// <summary>
        /// 单例
        /// </summary>
        public static DictionaryExportProvider Instance => Singleton<DictionaryExportProvider>.Instance;

        /// <summary>
        /// 导出表册
        /// </summary>
        /// <param name="findCellByName">通过名称查找单元格</param>
        /// <param name="main">数据</param>
        public override void ExportMain(Func<string, ICell> findCellByName, Dictionary<string, object> main)
        {
            foreach (var kv in main)
            {
                ICell cell = findCellByName(kv.Key);

                if (cell == null) continue;

                ValueProvider.SetValue(cell, kv.Value);
            }
        }
        /// <summary>
        /// 导出表册
        /// </summary>
        /// <param name="findCellByName">通过名称查找单元格</param>
        /// <param name="item">数据</param>
        public override void ExportChild(Func<string, ICell> findCellByName, Dictionary<string, object> item)
        {
            foreach (var kv in item)
            {
                ICell cell = findCellByName(kv.Key);

                if (cell == null) continue;

                ValueProvider.SetValue(cell, kv.Value);
            }
        }
    }
}

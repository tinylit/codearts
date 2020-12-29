using CodeArts.Runtime;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;

namespace CodeArts.NPOI.Xls.Exports
{
    /// <summary>
    /// 实体输出。
    /// </summary>
    /// <typeparam name="T">数据类型。</typeparam>
    public class EntryExportProvider<T> : ExportProvider<T>
    {
        /// <summary>
        /// 输出。
        /// </summary>
        /// <param name="findCellByName">通过名称查找单元格。</param>
        /// <param name="value">实体。</param>
        public override void Export(Func<string, ICell> findCellByName, T value)
        {
            var entry = TypeItem.Get<T>();

            entry.PropertyStores.ForEach(item =>
            {
                var cell = findCellByName(item.Name);

                if (cell is null) return;

                ValueProvider.SetValue(cell, item.Member.GetValue(value, null));
            });
        }
    }
}

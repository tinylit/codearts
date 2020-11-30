using NPOI.SS.UserModel;
using NPOI.SS.Util;
using System;
using System.Collections.Generic;

namespace CodeArts.NPOI.Xls
{
    /// <summary>
    /// 输出数据表。
    /// </summary>
    /// <typeparam name="T">表册行数据来源。</typeparam>
    public abstract class ExportProvider<T> : IExportProvider<T>
    {
        private IValueProvider provider;

        /// <summary>
        /// 当前行。
        /// </summary>
        public IRow RowCurrent { get; set; }

        /// <summary>
        /// 当前表。
        /// </summary>
        protected ISheet SheetCurrent { get { return RowCurrent.Sheet; } }

        /// <summary>
        /// 值提供者。
        /// </summary>
        public IValueProvider ValueProvider { get => provider ?? DefaultValueProvider.Instance; set => provider = value; }

        /// <summary>
        /// 输出。
        /// </summary>
        /// <param name="findCellByName">查找对应单元格。</param>
        /// <param name="value">当前行数据。</param>
        public abstract void Export(Func<string, ICell> findCellByName, T value);
    }
    /// <summary>
    /// 输出数据表(数据来源于字典型数据)。
    /// </summary>
    /// <typeparam name="TKey">字典Key类型。</typeparam>
    /// <typeparam name="TValue">字典Value类型。</typeparam>
    public abstract class ExportProvider<TKey, TValue> : ExportProvider<KeyValuePair<TKey, TValue>>, IExportProvider<TKey, TValue>
    {
        /// <summary>
        /// 设置单元格数据。
        /// </summary>
        /// <param name="findCellByName">查找对应单元格。</param>
        /// <param name="value">当前行数据。</param>
        public override void Export(Func<string, ICell> findCellByName, KeyValuePair<TKey, TValue> value)
        {
            if (value.Key == null) return;

            ExportMain(findCellByName, value.Key);

            if (value.Value == null) return;

            ExportChild(findCellByName, value.Value);
        }

        /// <summary>
        /// 设置主数据。
        /// </summary>
        /// <param name="findCellByName">查找对应单元格。</param>
        /// <param name="main">主数据。</param>
        public abstract void ExportMain(Func<string, ICell> findCellByName, TKey main);

        /// <summary>
        /// 设置关联数据。
        /// </summary>
        /// <param name="findCellByName">查找对应单元格。</param>
        /// <param name="item">关联数据。</param>
        public abstract void ExportChild(Func<string, ICell> findCellByName, TValue item);
    }

    /// <summary>
    /// 输出主辅列表提供者接口。
    /// </summary>
    /// <typeparam name="TKey">字典Key类型。</typeparam>
    /// <typeparam name="TCollect">字典Value集合类型。</typeparam>
    /// <typeparam name="T">字典Value集合类型的元素类型。</typeparam>
    public abstract class ExportProvider<TKey, TCollect, TItem> : ExportProvider<TKey, TCollect>, IExportProvider<KeyValuePair<TKey, TCollect>> where TCollect : IEnumerable<TItem>
    {
        /// <summary>
        /// 设置集合中的数据。
        /// </summary>
        /// <param name="findCellByName">查找对应单元格。</param>
        /// <param name="item">集合元素。</param>
        public abstract void ExportChild(Func<string, ICell> findCellByName, TItem item);
        /// <summary>
        /// 设置集合数据。
        /// </summary>
        /// <param name="findCellByName">查找对应单元格。</param>
        /// <param name="collect">集合。</param>
        public override void ExportChild(Func<string, ICell> findCellByName, TCollect collect)
        {
            int firstRow = RowCurrent.RowNum + 1,
                lastCol = RowCurrent.Cells.Count - 1;

            foreach (var item in collect)
            {
                ExportChild(findCellByName, item);

                RowCurrent = SheetCurrent.CreateRow(RowCurrent.RowNum + 1);
            }

            if (RowCurrent.RowNum > firstRow)
            {
                SheetCurrent.AddMergedRegion(new CellRangeAddress(firstRow, RowCurrent.RowNum - 1, 0, lastCol));
            }
        }

        /// <summary>
        /// 设置数据。
        /// </summary>
        /// <param name="findCellByName">查找对应单元格。</param>
        /// <param name="value">数据。</param>
        public override void Export(Func<string, ICell> findCellByName, KeyValuePair<TKey, TCollect> value)
        {
            if (value.Key == null) return;

            ExportMain(findCellByName, value.Key);

            if (value.Value == null) return;

            int firstRow = RowCurrent.RowNum + 1,
                lastCol = RowCurrent.Cells.Count - 1;
            bool append = false;

            foreach (var item in value.Value)
            {
                if (append)
                {
                    RowCurrent = SheetCurrent.CreateRow(RowCurrent.RowNum + 1);
                }
                else
                {
                    append = true;
                }
                ExportChild(findCellByName, item);
            }
            if (RowCurrent.RowNum > firstRow)
            {
                SheetCurrent.AddMergedRegion(new CellRangeAddress(firstRow, RowCurrent.RowNum, 0, lastCol));
            }
        }
    }
}

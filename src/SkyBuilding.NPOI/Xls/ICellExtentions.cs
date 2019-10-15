using NPOI.SS.UserModel;
using System;

namespace SkyBuilding.NPOI.Xls
{
    /// <summary>
    /// Cell 扩展
    /// </summary>
    public static class ICellExtentions
    {
        /// <summary>
        /// 创建下一个格子
        /// </summary>
        /// <param name="cell">单元格</param>
        /// <returns></returns>
        public static ICell CreateNextCell(this ICell cell)
        {
            return cell.Row.CreateCell(cell.ColumnIndex + 1);
        }

        /// <summary>
        /// 为单元格设置样式
        /// </summary>
        /// <param name="cell">单元格</param>
        /// <param name="header">表头样式</param>
        public static void StyleCb(this ICell cell, IHeaderProvider header)
        {
            if (cell == null)
            {
                throw new ArgumentNullException(nameof(cell));
            }

            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            var sheet = cell.Sheet;

            if (header.HorizontalAlignment.HasValue || header.VerticalAlignment.HasValue)
            {
                var style = cell.CellStyle;
                if (Equals(style, default(ICellStyle)))
                {
                    style = sheet.Workbook.CreateCellStyle();
                }
                if (header.HorizontalAlignment.HasValue)
                {
                    style.Alignment = header.HorizontalAlignment.Value;
                }
                if (header.VerticalAlignment.HasValue)
                {
                    style.VerticalAlignment = header.VerticalAlignment.Value;
                }
                cell.CellStyle = style;
            }

            if (header.Width.HasValue)
            {
                sheet.SetColumnWidth(cell.ColumnIndex, 32 * header.Width.Value);
            }
        }

        /// <summary>
        /// 设置内容居中的单元格
        /// </summary>
        /// <param name="cell">单元格</param>
        public static void StyleCenter(this ICell cell)
        {
            if (cell == null)
            {
                throw new ArgumentNullException(nameof(cell));
            }

            var style = cell.CellStyle;
            if (Equals(style, default(ICellStyle)))
            {
                style = cell.Sheet.Workbook.CreateCellStyle();
            }
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;

            cell.CellStyle = style;
        }
    }
}

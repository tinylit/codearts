using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Data;

namespace CodeArts.NPOI.Xls
{
    /// <summary>
    /// Sheet 扩展。
    /// </summary>
    public static class ISheetExtentions
    {
        /// <summary>
        /// 创建下一行。
        /// </summary>
        /// <param name="row">当前行。</param>
        /// <returns></returns>
        public static IRow CreateNextRow(this ISheet sheet)
        {
            if (sheet is null)
            {
                throw new ArgumentNullException(nameof(sheet));
            }

            return sheet.CreateRow(sheet.LastRowNum + 1);
        }

        /// <summary>
        /// 创建表头。
        /// </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        /// <param name="sheet">表。</param>
        /// <param name="collection">集合。</param>
        /// <param name="factory">工厂。</param>
        /// <returns></returns>
        public static Dictionary<string, int> CreateHeadRow<T>(this ISheet sheet, IEnumerable<T> collection, Func<T, ICell, string> factory)
        {
            if (sheet is null)
            {
                throw new ArgumentNullException(nameof(sheet));
            }

            if (collection is null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var dic = new Dictionary<string, int>();

            var row = sheet.CreateRow(sheet.LastRowNum + 1);

            foreach (T item in collection)
            {
                var cell = row.CreateNextCell();

                var name = factory.Invoke(item, cell);

                dic.Add(name.ToLower(), cell.ColumnIndex);
            }

            return dic;
        }

        /// <summary>
        /// 创建列头。
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, int> CreateHeadRow(this ISheet sheet, IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            return sheet.CreateHeadRow(keyValues, (item, cell) =>
            {
                cell.SetCellValue(item.Value);

                cell.StyleCenter();

                return item.Key;
            });
        }

        /// <summary>
        /// 创建列头。
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, int> CreateHeadRow<T>(this ISheet sheet, IEnumerable<KeyValuePair<string, T>> keyValues) where T : IHeaderProvider
        {
            return sheet.CreateHeadRow(keyValues, (item, cell) =>
            {
                var provider = item.Value;

                cell.SetCellValue(provider.Alias ?? item.Key ?? $"列{cell.ColumnIndex}");

                cell.StyleCb(provider);

                return item.Key;
            });
        }


        /// <summary>
        /// 创建列头。
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, ICell> CreateHeadRow(this ISheet sheet, DataColumnCollection keyValues)
        {
            var dic = new Dictionary<string, ICell>();

            var row = sheet.CreateRow(sheet.LastRowNum + 1);

            foreach (DataColumn item in keyValues)
            {
                var cell = row.CreateNextCell();

                cell.SetCellValue(item.ColumnName);

                cell.StyleCenter();

                dic.Add(item.ColumnName, cell);
            }

            return dic;
        }
    }
}

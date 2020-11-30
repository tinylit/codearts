using NPOI.SS.UserModel;
using CodeArts.NPOI.Xls;
using CodeArts.NPOI.Xls.Exports;
using System;
using System.Collections.Generic;
using System.Data;

namespace CodeArts.NPOI
{
    /// <summary>
    /// 表扩展。
    /// </summary>
    public static class SheetExtentions
    {
        public static ISheet Append(this ISheet sheet, DataTable source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            IRow row = sheet.CreateNextRow();

            foreach (DataColumn item in source.Columns)
            {
                var cell = row.CreateNextCell();

                cell.SetCellValue(item.ColumnName);

                var style = cell.CellStyle;
                if (Equals(style, default(ICellStyle)))
                {
                    style = sheet.Workbook.CreateCellStyle();
                }
                style.Alignment = HorizontalAlignment.Center;
                style.VerticalAlignment = VerticalAlignment.Center;

                cell.CellStyle = style;
            }

            var valueProvider = DefaultValueProvider.Instance;

            var len = source.Columns.Count;

            foreach (DataRow dr in source.Rows)
            {
                row = sheet.CreateNextRow();

                for (int i = 0; i < len; i++)
                {
                    valueProvider.SetValue(row.CreateCell(i), dr[i]);
                }
            }

            return sheet;
        }
        
        public static ISheet Append<TProvider>(this ISheet sheet, DataTable source, Dictionary<string, TProvider> aliaWithProviders) where TProvider : IDataProvider
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (aliaWithProviders is null)
            {
                throw new ArgumentNullException(nameof(aliaWithProviders));
            }

            var dic = new Dictionary<string, TProvider>();

            IRow row = sheet.CreateRow(0);

            bool flag = false;

            foreach (var item in aliaWithProviders)
            {
                flag = false;
                foreach (DataColumn column in source.Columns)
                {
                    if (string.Equals(item.Key, column.ColumnName, StringComparison.OrdinalIgnoreCase))
                    {
                        dic.Add(column.ColumnName, item.Value);
                        flag = true;
                        break;
                    }
                }

                if (!flag) continue;

                var cell = row.CreateNextCell();

                cell.StyleCb(item.Value);

                cell.SetCellValue(item.Key);
            }

            foreach (DataRow dr in source.Rows)
            {
                row = sheet.CreateNextRow();

                dic.ForEach((kv, index) =>
                {
                    kv.Value.SetValue(row.CreateCell(index), dr[kv.Key]);
                });
            }

            return sheet;
        }

        public static ISheet Append<T>(this ISheet sheet, IEnumerable<T> source, IExportProvider<T> provider, Dictionary<string, string> alias)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (alias is null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            var nameKeys = sheet.CreateHeadRow(alias);

            source.ForEach(item =>
            {
                provider.RowCurrent = sheet.CreateNextRow();
                provider.Export(key =>
                {
                    if (nameKeys.TryGetValue(key.ToLower(), out int index))
                        return provider.RowCurrent.GetCell(index, MissingCellPolicy.CREATE_NULL_AS_BLANK);//RETURN_NULL_AND_BLANK,RETURN_BLANK_AS_NULL

                    return null;
                }, item);
            });

            return sheet;
        }

        public static ISheet Append(this ISheet sheet, IEnumerable<Dictionary<string, object>> source, Dictionary<string, string> alias)
         => sheet.Append(source, KeyValuePairExportProvider.Instance, alias);
        
        public static ISheet Append(this ISheet sheet, IDictionary<Dictionary<string, object>, Dictionary<string, object>> source, Dictionary<string, string> alias)
            => sheet.Append(source, DictionaryExportProvider.Instance, alias);
        
        public static ISheet Append(this ISheet sheet, IDictionary<Dictionary<string, object>, List<Dictionary<string, object>>> source, Dictionary<string, string> alias)
            => sheet.Append(source, DictionaryListExportProvider.Instance, alias);

        public static ISheet Append<T, TProvider>(this ISheet sheet, IEnumerable<T> source, IExportProvider<T> provider, Dictionary<string, TProvider> aliaWithProviders) where TProvider : IDataProvider
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (aliaWithProviders is null)
            {
                throw new ArgumentNullException(nameof(aliaWithProviders));
            }

            var nameKeys = sheet.CreateHeadRow(aliaWithProviders);

            source.ForEach(item =>
            {
                provider.RowCurrent = sheet.CreateNextRow();
                provider.Export(key =>
                {
                    if (!nameKeys.TryGetValue(key.ToLower(), out int index))
                        return null;

                    aliaWithProviders.TryGetValue(key.ToLower(), out TProvider dataProvider);

                    provider.ValueProvider = dataProvider;

                    return provider.RowCurrent.GetCell(index, MissingCellPolicy.CREATE_NULL_AS_BLANK);
                }, item);
            });

            return sheet;
        }
 
        public static ISheet Append<TProvider>(this ISheet sheet, IEnumerable<Dictionary<string, object>> source, Dictionary<string, TProvider> aliaWithProviders)
            where TProvider : IDataProvider
            => sheet.Append(source, KeyValuePairExportProvider.Instance, aliaWithProviders);
        
        public static ISheet Append<TProvider>(this ISheet sheet, IDictionary<Dictionary<string, object>, Dictionary<string, object>> source, Dictionary<string, TProvider> aliaWithProviders)
            where TProvider : IDataProvider
            => sheet.Append(source, DictionaryExportProvider.Instance, aliaWithProviders);
        
        public static ISheet Append<TProvider>(this ISheet sheet, IDictionary<Dictionary<string, object>, List<Dictionary<string, object>>> source, Dictionary<string, TProvider> aliaWithProviders)
            where TProvider : IDataProvider
            => sheet.Append(source, DictionaryListExportProvider.Instance, aliaWithProviders);
    }
}

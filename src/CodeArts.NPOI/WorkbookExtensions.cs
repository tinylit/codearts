using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using CodeArts.NPOI.Xls;
using CodeArts.NPOI.Xls.Exports;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace CodeArts.NPOI
{
    /// <summary>
    /// Workbook 扩展。
    /// </summary>
    public static class WorkbookExtensions
    {
        /// <summary>
        /// 转byte字节流。
        /// </summary>
        /// <param name="workbook">办公文件。</param>
        /// <returns></returns>
        public static byte[] ToBytes(this IWorkbook workbook)
        {
            if (workbook is null)
            {
                throw new ArgumentNullException(nameof(workbook));
            }
            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// 生成Xls文件实体。
        /// </summary>
        /// <param name="source">实体对象。</param>
        /// <param name="sheetName">表名称。</param>
        /// <returns></returns>
        public static IWorkbook AsWorkbook(this DataTable source, string sheetName)
            => new XSSFWorkbook().Append(sheetName, source);

        /// <summary>
        /// 生成Xls文件实体。
        /// </summary>
        /// <param name="source">实体对象。</param>
        /// <param name="sheetName">表名称。</param>
        /// <param name="aliaWithProviders">列别名（设置时，与keyValues中的key相同项的value作为列名excel，并且只会生成包含于alias中的数据列）。</param>
        /// <returns></returns>
        public static IWorkbook AsWorkbook<TProvider>(this DataTable source, string sheetName, Dictionary<string, TProvider> aliaWithProviders) where TProvider : IDataProvider
            => new XSSFWorkbook().Append(sheetName, source, aliaWithProviders);

        /// <summary>
        /// 生成Xls文件实体。
        /// </summary>
        /// <param name="source">实体对象。</param>
        /// <param name="sheetName">表名称。</param>
        /// <param name="alias">列别名（设置时，与keyValues中的key相同项的value作为列名excel，并且只会生成包含于alias中的数据列）。</param>
        /// <returns></returns>
        public static IWorkbook AsWorkbook<T>(this IEnumerable<T> source, string sheetName, IExportProvider<T> provider, Dictionary<string, string> alias)
            => new XSSFWorkbook().Append(sheetName, source, provider, alias);

        /// <summary>
        /// 生成Xls文件实体。
        /// </summary>
        /// <param name="source">实体对象。</param>
        /// <param name="sheetName">表名称。</param>
        /// <param name="alias">列别名（设置时，与keyValues中的key相同项的value作为列名excel，并且只会生成包含于alias中的数据列）。</param>
        /// <returns></returns>
        public static IWorkbook AsWorkbook(this IEnumerable<Dictionary<string, object>> source, string sheetName, Dictionary<string, string> alias)
            => source.AsWorkbook(sheetName, KeyValuePairExportProvider.Instance, alias);

        /// <summary>
        /// 生成Xls文件实体。
        /// </summary>
        /// <param name="source">实体对象。</param>
        /// <param name="sheetName">表名称。</param>
        /// <param name="alias">列别名（设置时，与keyValues中的key相同项的value作为列名excel，并且只会生成包含于alias中的数据列）。</param>
        /// <returns></returns>
        public static IWorkbook AsWorkbook(this IDictionary<Dictionary<string, object>, Dictionary<string, object>> source, string sheetName, Dictionary<string, string> alias)
            => source.AsWorkbook(sheetName, DictionaryExportProvider.Instance, alias);

        /// <summary>
        /// 生成Xls文件实体。
        /// </summary>
        /// <param name="source">实体对象。</param>
        /// <param name="sheetName">表名称。</param>
        /// <param name="alias">列别名（设置时，与keyValues中的key相同项的value作为列名excel，并且只会生成包含于alias中的数据列）。</param>
        /// <returns></returns>
        public static IWorkbook AsWorkbook(this IDictionary<Dictionary<string, object>, List<Dictionary<string, object>>> source, string sheetName, Dictionary<string, string> alias)
            => source.AsWorkbook(sheetName, DictionaryListExportProvider.Instance, alias);

        /// <summary>
        /// 生成Xls文件实体。
        /// </summary>
        /// <param name="source">实体对象。</param>
        /// <param name="sheetName">表名称。</param>
        /// <param name="aliaWithProviders">列别名（设置时，与keyValues中的key相同项的value作为列名excel，并且只会生成包含于alias中的数据列）。</param>
        /// <returns></returns>
        public static IWorkbook AsWorkbook<T, TProvider>(this IEnumerable<T> source, string sheetName, IExportProvider<T> provider, Dictionary<string, TProvider> aliaWithProviders)
            where TProvider : IDataProvider
            => new XSSFWorkbook().Append(sheetName, source, provider, aliaWithProviders);

        /// <summary>
        /// 生成Xls文件实体。
        /// </summary>
        /// <param name="source">实体对象。</param>
        /// <param name="sheetName">表名称。</param>
        /// <param name="aliaWithProviders">列别名（设置时，与keyValues中的key相同项的value作为列名excel，并且只会生成包含于alias中的数据列）。</param>
        /// <returns></returns>
        public static IWorkbook AsWorkbook<TProvider>(this IEnumerable<Dictionary<string, object>> source, string sheetName, Dictionary<string, TProvider> aliaWithProviders)
            where TProvider : IDataProvider
            => source.AsWorkbook(sheetName, KeyValuePairExportProvider.Instance, aliaWithProviders);

        /// <summary>
        /// 生成Xls文件实体。
        /// </summary>
        /// <param name="source">实体对象。</param>
        /// <param name="sheetName">表名称。</param>
        /// <param name="aliaWithProviders">列别名（设置时，与keyValues中的key相同项的value作为列名excel，并且只会生成包含于alias中的数据列）。</param>
        /// <returns></returns>
        public static IWorkbook AsWorkbook<TProvider>(this IDictionary<Dictionary<string, object>, Dictionary<string, object>> source, string sheetName, Dictionary<string, TProvider> aliaWithProviders)
            where TProvider : IDataProvider
            => source.AsWorkbook(sheetName, DictionaryExportProvider.Instance, aliaWithProviders);

        /// <summary>
        /// 生成Xls文件实体。
        /// </summary>
        /// <param name="source">实体对象。</param>
        /// <param name="sheetName">表名称。</param>
        /// <param name="aliaWithProviders">列别名（设置时，与keyValues中的key相同项的value作为列名excel，并且只会生成包含于alias中的数据列）。</param>
        /// <returns></returns>
        public static IWorkbook AsWorkbook<TProvider>(this IDictionary<Dictionary<string, object>, List<Dictionary<string, object>>> source, string sheetName, Dictionary<string, TProvider> aliaWithProviders)
            where TProvider : IDataProvider
            => source.AsWorkbook(sheetName, DictionaryListExportProvider.Instance, aliaWithProviders);

        public static IWorkbook Append(this IWorkbook workbook, string sheetName, DataTable source)
        {
            if (sheetName is null)
            {
                throw new ArgumentNullException(nameof(sheetName));
            }

            ISheet sheet = workbook.CreateSheet(sheetName);

            sheet.Append(source);

            return workbook;
        }

        public static IWorkbook Append<TProvider>(this IWorkbook workbook, string sheetName, DataTable source, Dictionary<string, TProvider> aliaWithProviders) where TProvider : IDataProvider
        {
            if (sheetName is null)
            {
                throw new ArgumentNullException(nameof(sheetName));
            }

            ISheet sheet = workbook.CreateSheet(sheetName);

            sheet.Append(source, aliaWithProviders);

            return workbook;
        }

        public static IWorkbook Append<T>(this IWorkbook workbook, string sheetName, IEnumerable<T> source, IExportProvider<T> provider, Dictionary<string, string> alias)
        {
            if (sheetName is null)
            {
                throw new ArgumentNullException(nameof(sheetName));
            }

            ISheet sheet = workbook.CreateSheet(sheetName);

            sheet.Append(source, provider, alias);

            return workbook;
        }

        public static IWorkbook Append(this IWorkbook workbook, string sheetName, IEnumerable<Dictionary<string, object>> source, Dictionary<string, string> alias)
        => workbook.Append(sheetName, source, KeyValuePairExportProvider.Instance, alias);

        public static IWorkbook Append(this IWorkbook workbook, string sheetName, IDictionary<Dictionary<string, object>, Dictionary<string, object>> source, Dictionary<string, string> alias)
            => workbook.Append(sheetName, source, DictionaryExportProvider.Instance, alias);

        public static IWorkbook Append(this IWorkbook workbook, string sheetName, IDictionary<Dictionary<string, object>, List<Dictionary<string, object>>> source, Dictionary<string, string> alias)
            => workbook.Append(sheetName, source, DictionaryListExportProvider.Instance, alias);

        public static IWorkbook Append<T, TProvider>(this IWorkbook workbook, string sheetName, IEnumerable<T> source, IExportProvider<T> provider, Dictionary<string, TProvider> aliaWithProviders) where TProvider : IDataProvider
        {
            if (sheetName is null)
            {
                throw new ArgumentNullException(nameof(sheetName));
            }

            ISheet sheet = workbook.CreateSheet(sheetName);

            sheet.Append(source, provider, aliaWithProviders);

            return workbook;
        }

        public static IWorkbook Append<TProvider>(this IWorkbook workbook, string sheetName, IEnumerable<Dictionary<string, object>> source, Dictionary<string, TProvider> aliaWithProviders)
            where TProvider : IDataProvider
            => workbook.Append(sheetName, source, KeyValuePairExportProvider.Instance, aliaWithProviders);

        public static IWorkbook Append<TProvider>(this IWorkbook workbook, string sheetName, IDictionary<Dictionary<string, object>, Dictionary<string, object>> source, Dictionary<string, TProvider> aliaWithProviders)
            where TProvider : IDataProvider
            => workbook.Append(sheetName, source, DictionaryExportProvider.Instance, aliaWithProviders);

        public static IWorkbook Append<TProvider>(this IWorkbook workbook, string sheetName, IDictionary<Dictionary<string, object>, List<Dictionary<string, object>>> source, Dictionary<string, TProvider> aliaWithProviders)
            where TProvider : IDataProvider
            => workbook.Append(sheetName, source, DictionaryListExportProvider.Instance, aliaWithProviders);
    }
}

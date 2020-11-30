using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;

namespace CodeArts.NPOI.Xls
{
    /// <summary>
    /// 输出数据表。
    /// </summary>
    /// <typeparam name="T">表册行数据来源。</typeparam>
    public interface IExportProvider<T>
    {
        /// <summary>
        /// 当前数据行。
        /// </summary>
        IRow RowCurrent { get; set; }
        /// <summary>
        /// 设置值。
        /// </summary>
        IValueProvider ValueProvider { get; set; }

        /// <summary>
        /// 输出当前数据。
        /// </summary>
        /// <param name="action">获取字段的列位置。</param>
        /// <param name="value">数据。</param>
        void Export(Func<string, ICell> action, T value);
    }

    /// <summary>
    /// 输出数据表(数据来源于字典型数据)。
    /// </summary>
    /// <typeparam name="TKey">字典Key类型。</typeparam>
    /// <typeparam name="TValue">字典Value类型。</typeparam>
    public interface IExportProvider<TKey, TValue> : IExportProvider<KeyValuePair<TKey, TValue>>
    {

    }


    /// <summary>
    /// 输出主辅列表提供者接口。
    /// </summary>
    /// <typeparam name="TKey">字典Key类型。</typeparam>
    /// <typeparam name="TCollect">字典Value集合类型。</typeparam>
    /// <typeparam name="T">字典Value集合类型的元素类型。</typeparam>
    public interface IExportProvider<TKey, TCollect, T> : IExportProvider<TKey, TCollect> where TCollect : IEnumerable<T>
    {

    }
}

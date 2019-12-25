using NPOI.SS.UserModel;

namespace CodeArts.NPOI.Xls
{
    /// <summary>
    /// 值提供者
    /// </summary>
    public interface IValueProvider
    {
        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="cell">单元格</param>
        /// <param name="value">数据</param>
        void SetValue(ICell cell, object value);
    }
}

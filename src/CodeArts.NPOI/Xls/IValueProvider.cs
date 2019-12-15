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
        /// <param name="value"></param>
        void SetValue(ICell cell, object value);
    }
}

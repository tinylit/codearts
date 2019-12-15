using NPOI.SS.UserModel;
using System;

namespace CodeArts.NPOI.Xls
{
    /// <summary>
    /// 默认值提供器
    /// </summary>
    public class DefaultValueProvider : DesignMode.Singleton<DefaultValueProvider>, IValueProvider
    {
        /// <summary>
        /// 私有构造函数
        /// </summary>
        private DefaultValueProvider() { }

        /// <summary>
        /// 设置值
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="value"></param>
        public void SetValue(ICell cell, object value)
        {
            if (value == null)
            {
                cell.SetCellValue(string.Empty);
            }
            else if (value is bool booleanValue)
            {
                cell.SetCellValue(booleanValue);
            }
            else if (value is DateTime dateTime)
            {
                cell.SetCellValue(dateTime);
            }
            else if (value is IRichTextString textString)
            {
                cell.SetCellValue(textString);
            }
            else if (value is double doubleValue)
            {
                cell.SetCellValue(doubleValue);
            }
            else
            {
                cell.SetCellValue(value.ToString());
            }
        }
    }
}

using NPOI.SS.UserModel;

namespace CodeArts.NPOI.Xls
{
    /// <summary>
    /// 构建表头
    /// </summary>
    public interface IHeaderProvider
    {
        /// <summary>
        /// 别名
        /// </summary>
        string Alias { get; set; }

        /// <summary>
        /// 单元格宽度(单位:像素)
        /// </summary>
        int? Width { get; set; }

        /// <summary>
        /// 垂直方向
        /// </summary>
        VerticalAlignment? VerticalAlignment { get; set; }

        /// <summary>
        /// 水平方向
        /// </summary>
        HorizontalAlignment? HorizontalAlignment { get; set; }
    }
}

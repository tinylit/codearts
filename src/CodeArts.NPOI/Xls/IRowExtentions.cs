using NPOI.SS.UserModel;

namespace CodeArts.NPOI.Xls
{
    /// <summary>
    /// Row 拓展类
    /// </summary>
    public static class IRowExtentions
    {
        /// <summary>
        /// 创建下一个格子
        /// </summary>
        /// <param name="cell">单元格</param>
        /// <returns></returns>
        public static ICell CreateNextCell(this IRow row)
        {
            return row.CreateCell(row.LastCellNum == -1 ? 0 : row.LastCellNum);
        }
    }
}

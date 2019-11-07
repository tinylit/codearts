using System.Collections.Generic;

namespace SkyBuilding.ORM
{
    /// <summary>
    /// SQL矫正设置
    /// </summary>
    public interface ISQLCorrectSimSettings
    {
        /// <summary>
        /// 数据库引擎
        /// </summary>
        DatabaseEngine Engine { get; }
        /// <summary>
        /// 字符串截取
        /// </summary>
        string Substring { get; }
        /// <summary>
        /// 索引位置
        /// </summary>
        string IndexOf { get; }
        /// <summary>
        /// 长度测量器
        /// </summary>
        string Length { get; }

        /// <summary>
        /// 索引交换位置（默认：value.indexOf("x")=>IndexOfMethod(value,"x")）
        /// </summary>
        bool IndexOfSwapPlaces { get; }

        /// <summary>
        /// 格式化集合（用作ISQL.ToString(ISQLCorrectSimSettings settings)矫正SQL语句使用）。
        /// </summary>
        ICollection<IFormatter> Formatters { get; }

        /// <summary>
        /// 字段名称
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        string Name(string name);

        /// <summary>
        /// 参数名称
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        string ParamterName(string name);
    }
}

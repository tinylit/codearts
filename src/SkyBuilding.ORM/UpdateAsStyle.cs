namespace SkyBuilding.ORM
{
    /// <summary>
    /// 别名更新风格
    /// </summary>
    public enum UpdateAsStyle
    {
        /// <summary>
        /// 正常 （UPDATE x SET x.Name = "yep" FROM yep_users x WHERE x.Id = 10000000）
        /// </summary>
        Normal,
        /// <summary>
        /// MySql (UPDATE yep_users x SET x.Name = "yep" WHERE x.Id = 10000000)
        /// </summary>
        MySql
    }
}

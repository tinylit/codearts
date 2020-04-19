namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 更新能力。
    /// </summary>
    public class UpdateableAttribute : CommandAbleAttribute
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public UpdateableAttribute() : base(CommandKind.Update)
        {
        }
    }
}

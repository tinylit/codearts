using System;

namespace CodeArts.DbAnnotations
{
    /// <summary>
    /// 更新。
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class UpdateSetAttribute : ObjectiveAttribute
    {
    }
}

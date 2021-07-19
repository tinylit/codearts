using System.Collections.Generic;

namespace CodeArts.Db
{
    /// <summary>
    /// 自定义表达式集合。
    /// </summary>
    public class CustomVisitorList : List<ICustomVisitor>, ICustomVisitorList
    {
    }
}

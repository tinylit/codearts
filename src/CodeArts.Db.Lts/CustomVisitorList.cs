﻿using System.Collections.Generic;

namespace CodeArts.Db.Lts
{
    /// <summary>
    /// 自定义表达式集合。
    /// </summary>
    public class CustomVisitorList : List<ICustomVisitor>, ICustomVisitorList
    {
    }
}

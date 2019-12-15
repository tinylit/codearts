using System;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.ORM.Tests.Serialize
{
    /// <summary>
    /// 日期令牌
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class DateTimeTokenAttribute : TokenAttribute
    {
        public override object Create() => DateTime.Now;
    }
}

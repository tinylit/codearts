using System;
using System.Collections.Generic;
using System.Text;

namespace SkyBuilding.ORM.Tests
{
    public class SqlServerConsts
    {
        internal static readonly string Domain = Environment.GetEnvironmentVariable("DEV-DATABASE-SQLSEVER-DOMAIN");
        internal static readonly string Database = Environment.GetEnvironmentVariable("DEV-DATABASE-SQLSEVER-DATABASE");
        internal static readonly string User = Environment.GetEnvironmentVariable("DEV-DATABASE-SQLSEVER-USER");
        internal static readonly string Password = Environment.GetEnvironmentVariable("DEV-DATABASE-SQLSEVER-PASSWORD");
    }
}

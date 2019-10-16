using System;
using System.Collections.Generic;
using System.Text;

namespace SkyBuilding.ORM.Tests
{
    public class SqlServerConsts
    {
        internal static readonly string Domain = Environment.GetEnvironmentVariable("DEV-DATABASE-SQLSEVER-DOMAIN") ?? "local.server.com";
        internal static readonly string Database = Environment.GetEnvironmentVariable("DEV-DATABASE-SQLSEVER-DATABASE") ?? "yep_sky_orm";
        internal static readonly string User = Environment.GetEnvironmentVariable("DEV-DATABASE-SQLSEVER-USER") ?? "orm";
        internal static readonly string Password = Environment.GetEnvironmentVariable("DEV-DATABASE-SQLSEVER-PASSWORD") ?? "Password12!";
    }
}

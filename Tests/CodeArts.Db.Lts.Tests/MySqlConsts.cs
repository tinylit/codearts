using System;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.Db.Lts.Tests
{
    public class MySqlConsts
    {
        internal static readonly string Domain = Environment.GetEnvironmentVariable("DEV-DATABASE-MYSQL-DOMAIN") ?? "mysql.server.com";
        internal static readonly string Database = Environment.GetEnvironmentVariable("DEV-DATABASE-MYSQL-DATABASE") ?? "yep_sky_orm";
        internal static readonly string User = Environment.GetEnvironmentVariable("DEV-DATABASE-MYSQL-USER") ?? "root";
        internal static readonly string Password = Environment.GetEnvironmentVariable("DEV-DATABASE-MYSQL-PASSWORD") ?? "Password!12";
    }
}

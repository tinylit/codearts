using SkyBuilding.ORM.Tests.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using UnitTest.Serialize;

namespace SkyBuilding.ORM.Tests.Domain
{
    [DefaultDbConfig]
    public class OrmTestRepository : DbRepository<OrmTest>
    {
    }
}

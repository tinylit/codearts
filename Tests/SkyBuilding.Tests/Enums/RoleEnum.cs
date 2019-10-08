using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SkyBuilding.Tests.Enums
{
    public enum RoleEnum
    {
        User,
        [Description("管理员")]
        Admin
    }

    [Flags]
    public enum RoleFlagsEnum
    {
        User = 1 << 0,
        [Description("管理员")]
        Admin = 1 << 1
    }
}

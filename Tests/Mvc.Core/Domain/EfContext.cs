using CodeArts.Db;
using CodeArts.Db.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Mvc.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mvc.Core.Domain
{
    /// <summary>
    /// 上下文。
    /// </summary>
    [DbConfig("connectionStrings:mssql")]
    public class EfContext : DbContext<EfContext>
    {
        /// <summary>
        /// 用户。
        /// </summary>
        public DbSet<FeiUsers> Users { get; set; }
    }
}

using CodeArts.ORM.Tests;
using UnitTest.Domain.Entities;
using UnitTest.Serialize;

namespace CodeArts.ORM.Domain
{
    [SqlServerConnection]
    public class UserRepository : DbRepository<FeiUsers>
    {
        protected override bool QueryAuthorize(ISQL sql) => true;

        public ApplyDto GetApply()
        {
            var sql = @"select
            '6651287474607755264' as shopId,
            '东坡区圣丹大药房柳圣张红霞加盟店' as gmfmc,
            1646.35 as jshj,
            '2018-10-09' as ywrq,
            'FPBZDA00001909'  as ddbh,
            '0' as autoKp,
            '13795511366' as sprsjh,
            '' as spryx,
            '东坡区柳圣正街13795511366' as gmfdzdh,
            '511402600334395'  as gmfsbh,
            '' as gmfkhhjzh,
            '' as bz,
            '1' as invoiceType,
            '499098834059'  as machineCode";

            return QueryFirst<ApplyDto>(new SQL(sql));
        }
    }
}

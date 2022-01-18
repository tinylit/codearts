using CodeArts;
using CodeArts.Casting;
using CodeArts.Db;
using CodeArts.Db.Lts;
using CodeArts.Db.Lts.MySql;
using CodeArts.Db.Lts.Tests;
using CodeArts.Db.Tests.Domain;
using CodeArts.Db.Tests.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnitTest.Domain;
using UnitTest.Dtos;
using UnitTest.Enums;

namespace UnitTest
{
    [TestClass]
    public class MySqlTest
    {
        private static bool isCompleted;

        private MySqlUserRespository UserSingleton => Singleton<MySqlUserRespository>.Instance;
        private AuthShipRepository AuthShipSingleton => Singleton<AuthShipRepository>.Instance;
        private AuthTreeRepository AuthTreesingleton => Singleton<AuthTreeRepository>.Instance;
        private TaxCodeRepository TaxCodeSingleton => Singleton<TaxCodeRepository>.Instance;

        [TestInitialize]
        public void Initialize()
        {
            var adapter = new MySqlLtsAdapter();

            adapter.Visitors.Add(new ConvertVisitter());

            DbConnectionManager.RegisterAdapter(adapter);
            DbConnectionManager.RegisterDatabaseFor<DapperFor>();

            if (isCompleted) return;

            using (var startup = new XStartup())
            {
                startup.DoStartup();
            }

            var connectionString = string.Format("server={0};port=3306;user=root;password={1};database=mysql;"
                , MySqlConsts.Domain
                , MySqlConsts.Password);

            using (var connection = TransactionConnections.GetConnection(connectionString, adapter) ?? DispatchConnections.Instance.GetConnection(connectionString, adapter))
            {
                try
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = System.Data.CommandType.Text;

                        var files = Directory.GetFiles("../../../Sql", "*.sql");

                        foreach (var file in files.Where(x => x.Contains("mysql")))
                        {
                            string text = File.ReadAllText(file, Encoding.UTF8);

                            command.CommandText = text;

                            command.ExecuteNonQuery();
                        }
                    }

                    isCompleted = true;
                }
                finally
                {
                    connection.Close();
                }
            }

        }

        [TestMethod]
        public void SelectTest()
        {
            var id = 6518750666955952128uL;
            var rights = new int[] { 1, 2, 3, 4, 5 };
            var userRights = AuthShipSingleton.Where(x => x.Status != CommonStatusEnum.Deleted && x.Type == AuthShipEnum.Tree || x.Type == AuthShipEnum.Vip && x.OwnerId == id && rights.Contains(x.AuthId));

            var auths = AuthTreesingleton.Where(x => x.Status != CommonStatusEnum.Deleted && rights.Contains(x.Id) && !userRights.Any(y => y.AuthId == x.Id))
                .ToList();
        }

        [TestMethod]
        public void SelectNullableTest()
        {
            var role = new UserRole?(UserRole.Owner);
            var defautUser = UserSingleton.Where(x => x.Role == role.Value).FirstOrDefault();
        }

        [TestMethod]
        public void IsNullableTest()
        {
            var defautUser = UserSingleton.Where(x => x.Name == null).FirstOrDefault();
        }

        [TestMethod]
        public void SelectOrTest()
        {
            UserInDto user = new UserInDto
            {
                OrgId = 6518750666955952000,
                CompanyId = 6518750666955952000,
                Account = "hyl",
                Role = UserRole.Owner,
                Name = "何远利",
                Tel = "18980861011",
                Mail = "yepsharp@gmail.com",
                Sex = UserSex.Male,
                Password = "123456",
                Status = CommonStatusEnum.Enabled
            };

            bool validTel = !string.IsNullOrEmpty(user.Tel);

            bool validMail = !string.IsNullOrEmpty(user.Mail);

            bool validWx = !string.IsNullOrEmpty(user.WechatId);

            bool validAlipay = !string.IsNullOrEmpty(user.AlipayId);

            var data = from x in UserSingleton
                       where validWx && x.WechatId == user.WechatId
                       select x;
            var value = data.FirstOrDefault();


            var defautUser = UserSingleton.Where(x => validWx && x.WechatId == user.WechatId)
               .FirstOrDefault();

            var dataUser = UserSingleton.Where(x => x.Account == user.Account ||
                (validTel && x.Tel == user.Tel) || // 电话
                (validMail && x.Mail == user.Mail) || // 邮件
                (validWx && x.WechatId == user.WechatId) || // 微信账号
                (validAlipay && x.AlipayId == user.AlipayId)) // 支付宝账户
                .FirstOrDefault();
        }

        [TestMethod]
        public void FirstOrDefault()
        {
            var list = new List<int> { 0 };

            var code = AuthTreesingleton.Where(x => x.ParentId == list[0])
                 .Select(x => x.Code)
                 .FirstOrDefault();
        }

        [TestMethod]
        public void InsertEntityTest()
        {
            var modified = DateTime.Now;

            var entry = new Domain.Entities.User
            {
                Id = (ulong)DateTime.Now.Ticks,
                OrgId = 6518750666955952000,
                CompanyId = 6518750666955952000,
                Account = "hyl",
                Role = UserRole.Owner,
                Name = "何远利",
                Tel = "18980861011",
                Mail = "yepsharp@gmail.com",
                Sex = UserSex.Male,
                Password = "$.xxx.1234",
                Status = CommonStatusEnum.Enabled,
                Registered = DateTime.Now,
                Modified = modified
            };

            int insertCount = UserSingleton.AsInsertable(entry)
                  .ExecuteCommand();

            int deleteCount = UserSingleton.AsDeleteable(new Domain.Entities.User
            {
                Id = entry.Id
            })
            .SkipIdempotentValid()
            .ExecuteCommand();

            Assert.AreEqual(deleteCount, insertCount);
        }

        [TestMethod]
        public void UpdateTest()
        {
            int i = UserSingleton
             .Where(x => x.Account.Contains("admin"))
             .Update(x => new Domain.Entities.User
             {
                 Account = "admin"
             });
        }

        [TestMethod]
        public void UpdateEntityTest()
        {
            var modified = DateTime.Now;

            var entry = new Domain.Entities.User
            {
                Id = (ulong)DateTime.Now.Ticks,
                OrgId = 6518750666955952000,
                CompanyId = 6518750666955952000,
                Account = "hyl",
                Role = UserRole.Owner,
                Name = "何远利",
                Tel = "18980861011",
                Mail = "yepsharp@gmail.com",
                Sex = UserSex.Male,
                Password = "$.xxx.1234",
                Status = CommonStatusEnum.Enabled,
                Registered = DateTime.Now,
                Modified = modified
            };

            UserSingleton.AsUpdateable(entry)
            .Set(x => x.Account)
            .Where(x => new { x.Tel, x.Mail })
            .ExecuteCommand();
        }

        [TestMethod]
        public void DeleteTest()
        {
            int i = UserSingleton
             .Where(x => x.Id == 1)
             .Delete();
        }

        [TestMethod]
        public void DeleteEntityTest()
        {
            var modified = DateTime.Now;

            var entry = new Domain.Entities.User
            {
                Id = (ulong)DateTime.Now.Ticks,
                OrgId = 6518750666955952000,
                CompanyId = 6518750666955952000,
                Account = "hyl",
                Role = UserRole.Owner,
                Name = "何远利",
                Tel = "18980861011",
                Mail = "yepsharp@gmail.com",
                Sex = UserSex.Male,
                Password = "$.xxx.1234",
                Status = CommonStatusEnum.Enabled,
                Registered = DateTime.Now,
                Modified = modified
            };

            int i = UserSingleton.AsDeleteable(entry)
            .Where(x => x.Account)
            .ExecuteCommand();
        }

        [TestMethod]
        public void HasChildTest()
        {
            string id = "*";
            bool isLevel = string.IsNullOrEmpty(id) || id == "*";
            var list = TaxCodeSingleton.Where(x => isLevel ? x.Level == 1 : x.ParentId == id)
                .Select(x => new
                {
                    Id = Convert.ToString(x.Id),
                    x.Name,
                    x.ShortName,
                    HasChild = TaxCodeSingleton.Any(y => y.ParentId == x.Id)
                }).ToList();
        }

        [TestMethod]
        public void OrmTest()
        {
            var ormTest = new OrmTestRepository();

            var list = new List<OrmTest>();

            for (int i = 0; i < 2000; i++)
            {
                list.Add(new OrmTest
                {
                    Id = (i + 1) * 100000 + (i + 1) * 10000 + i
                });
            }

            int insertCount = ormTest.AsInsertable(list).ExecuteCommand();

            var results = ormTest.ToList();

            int deleteCount = ormTest.AsDeleteable(results)
                .SkipIdempotentValid()
                .ExecuteCommand();

            Assert.AreEqual(insertCount, deleteCount);
        }

        [TestMethod]
        public void ConcatTest()
        {
            var userDto = UserSingleton.Where(x => x.Name.StartsWith(x.Account + x.Name))
                .DefaultIfEmpty(new Domain.Entities.User
                {
                    Id = 1
                })
                .First();
        }
    }
}

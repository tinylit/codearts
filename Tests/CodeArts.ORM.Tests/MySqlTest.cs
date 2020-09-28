using CodeArts;
using CodeArts.MySql;
using CodeArts.ORM;
using CodeArts.ORM.Tests.Domain;
using CodeArts.ORM.Tests.Domain.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnitTest.Domain;
using UnitTest.Dtos;
using UnitTest.Enums;
using UnitTest.Visiters;

namespace UnitTest
{
    [TestClass]
    public class MySqlTest
    {
        private static bool isCompleted;

        private readonly MySqlUserRespository userSingleton = Singleton<MySqlUserRespository>.Instance;
        private readonly AuthShipRepository authShipSingleton = Singleton<AuthShipRepository>.Instance;
        private readonly AuthTreeRepository authTreesingleton = Singleton<AuthTreeRepository>.Instance;
        private readonly TaxCodeRepository taxCodeSingleton = Singleton<TaxCodeRepository>.Instance;

        [TestInitialize]
        public void Initialize()
        {
            var adapter = new MySqlAdapter();

            adapter.Settings.Visitors.Add(new ConvertVisitter());

            DbConnectionManager.RegisterAdapter(adapter);
            DbConnectionManager.RegisterProvider<CodeArtsProvider>();

            if (isCompleted) return;

            var connectionString = "server=127.0.0.1;port=3306;user=root;password=Password12!;database=mysql;";

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
            var userRights = authShipSingleton.Where(x => x.Status != CommonStatusEnum.Deleted && x.Type == AuthShipEnum.Tree || x.Type == AuthShipEnum.Vip && x.OwnerId == id && rights.Contains(x.AuthId));

            var auths = authTreesingleton.Where(x => x.Status != CommonStatusEnum.Deleted && rights.Contains(x.Id) && !userRights.Any(y => y.AuthId == x.Id))
                .ToList();
            /**
             * SELECT `x`.`id`, `x`.`parent_id` AS `ParentId`, `x`.`disp_order` AS `DispOrder`, `x`.`has_child` AS `HasChild`, `x`.`code`, `x`.`name`, `x`.`url`, `x`.`type`, `x`.`status`, `x`.`created`, `x`.`modified` 
             * FROM `yep_auth_tree` `x` 
             * WHERE (((`x`.`status` <> ?__variable_1) 
             *      AND `x`.`id` IN (?__variable_2, ?__variable_3, ?__variable_4, ?__variable_5, ?__variable_6)) 
             *      AND NOT EXISTS(
             *          SELECT `x1`.`id` 
             *          FROM `yep_auth_ship` `x1` 
             *          WHERE (`x1`.`auth_id` = `x`.`id`)
             *      )
             * )
             */
        }

        [TestMethod]
        public void SelectNullableTest()
        {
            var role = new UserRole?(UserRole.Owner);
            var defautUser = userSingleton.Where(x => x.Role == role.Value).FirstOrDefault();
        }

        [TestMethod]
        public void IsNullableTest()
        {
            var defautUser = userSingleton.Where(x => x.Name == null).FirstOrDefault();
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

            var data = from x in userSingleton
                       where validWx && x.WechatId == user.WechatId
                       select x;
            var value = data.FirstOrDefault();


            var defautUser = userSingleton.Where(x => validWx && x.WechatId == user.WechatId)
               .FirstOrDefault();

            /**
             * SELECT `org_id` AS `OrgId`, `company_id` AS `CompanyId`, `account`, `role`, `name`, `wechat_id` AS `WechatId`, `alipay_id` AS `AlipayId`, `tel`, `mail`, `avatar`, `sex`, `password`, `salt`, `status`, `extends_enum` AS `ExtendsEnum`, `registered`, `modified`, `id` 
             * FROM `yep_users` 
             * LIMIT 1
             */

            var dataUser = userSingleton.Where(x => x.Account == user.Account ||
                (validTel && x.Tel == user.Tel) || // 电话
                (validMail && x.Mail == user.Mail) || // 邮件
                (validWx && x.WechatId == user.WechatId) || // 微信账号
                (validAlipay && x.AlipayId == user.AlipayId)) // 支付宝账户
                .FirstOrDefault();
            /**
             *  SELECT `x`.`org_id` AS `OrgId`, `x`.`company_id` AS `CompanyId`, `x`.`account`, `x`.`role`, `x`.`name`, `x`.`wechat_id` AS `WechatId`, `x`.`alipay_id` AS `AlipayId`, `x`.`tel`, `x`.`mail`, `x`.`avatar`, `x`.`sex`, `x`.`password`, `x`.`salt`, `x`.`status`, `x`.`extends_enum` AS `ExtendsEnum`, `x`.`registered`, `x`.`modified`, `x`.`id` 
             *  FROM `yep_users` `x`
             *  WHERE (((`x`.`account` = ?Account) OR (`x`.`tel` = ?Tel)) OR (`x`.`mail` = ?Mail)) 
             *  LIMIT 1
             */
        }

        [TestMethod]
        public void FirstOrDefault()
        {
            var list = new List<int> { 0 };

            var code = authTreesingleton.Where(x => x.ParentId == list[0])
                 .Select(x => x.Code)
                 .FirstOrDefault();
        }


        [TestMethod]
        public void InsertTest()
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

            int i = userSingleton.AsDeleteable(entry)
                .Where(x => x.Account ?? x.Tel)
                .ExecuteCommand();

            userSingleton.AsExecuteable()
                .Where(x => x.Account.Contains("admin"))
                .Update(x => new Domain.Entities.User
                {
                    Account = "admin"
                });

            userSingleton.AsUpdateable(entry)
                .Where(x => new { x.Tel, x.Mail })
                .Limit(x => x.Account)
                .Except(x => x.Id)
                .ExecuteCommand();

            userSingleton.AsExecuteable()
                .Where(x => x.Id == 1)
                .Delete();

            userSingleton.AsDeleteable(new Domain.Entities.User[] { entry })
                .Where(x => x.Id)
                .ExecuteCommand();

            userSingleton.AsExecuteable()
                .Insert(userSingleton.Take(1).Select(x => x));

            userSingleton.AsInsertable(entry)
                .ExecuteCommand();

            var entity = userSingleton
                .Where(x => x.Account == "hyl")
                .FirstOrDefault();

            entity.Tel = "18980861111";
            entity.Mail = "tinylit@foxmail.com";

            int k = userSingleton.AsUpdateable(entity)
                .Limit(x => new { x.Tel, x.Mail })
                .Where(x => x.Account)
                .ExecuteCommand();

            var data = userSingleton.Where(x => x.Mail == "tinylit@foxmail.com").FirstOrDefault();
        }

        [TestMethod]
        public void HasChildTest()
        {
            string id = "*";
            bool isLevel = string.IsNullOrEmpty(id) || id == "*";
            var list = taxCodeSingleton.Where(x => isLevel ? x.Level == 1 : x.ParentId == id)
                .Select(x => new
                {
                    Id = Convert.ToString(x.Id),
                    Name = x.Name,
                    ShortName = x.ShortName,
                    HasChild = taxCodeSingleton.Any(y => y.ParentId == x.Id)
                }).ToList();

            /**
             * SELECT 
             * CONVERT(`x`.`Id`,CHAR) AS `Id`,
             * `x`.`name` AS `Name`,
             * `x`.`short_name` AS `ShortName`,
             * (
             *  SELECT CASE 
             *  WHEN EXISTS(SELECT `y`.`Id` FROM `yep_tax_code` `y` WHERE (`y`.`parent_id`=`x`.`Id`)) 
             *      THEN ?__variable_true 
             *  ELSE ?__variable_false
             *  END
             * ) AS `HasChild` 
             * FROM `yep_tax_code` `x`
             * WHERE (`x`.`level`=?__variable_1)
             */
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
                    Id = (i + 1) * 1000000 + (i + 1) * 100000 + i
                });
            }

            var j = ormTest.AsInsertable(list).ExecuteCommand();

            var results = ormTest.ToList();

            var k = ormTest.AsDeleteable(list).ExecuteCommand();
        }

        [TestMethod]
        public void ConcatTest()
        {
            var userDto = userSingleton.Where(x => x.Name.StartsWith(x.Account + x.Name)).FirstOrDefault();
        }
    }
}

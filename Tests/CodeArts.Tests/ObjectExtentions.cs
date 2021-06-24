using CodeArts.Casting;
using CodeArts.Casting.Implements;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace CodeArts.Tests
{
    /// <summary>
    /// 用户角色。
    /// </summary>
    [Flags]
    public enum UserRoleEnum
    {
        #region 客户
        /// <summary>
        /// 用户。
        /// </summary>
        Normal = 1 << 0,

        /// <summary>
        /// 开票员。
        /// </summary>
        Kpr = 1 << 1,

        /// <summary>
        /// 默认开票员。
        /// </summary>
        DefaultKpr = 1 << 2,

        /// <summary>
        /// 代理开票员。
        /// </summary>
        ProxyKpr = 1 << 3,

        /// <summary>
        /// 代理管理员。
        /// </summary>
        ProxyAdministrator = 1 << 4,

        /// <summary>
        /// 管理员。
        /// </summary>
        Administrator = 1 << 5,

        /// <summary>
        /// 团队管理员。
        /// </summary>
        TeamAdministrator = 1 << 6,

        /// <summary>
        /// 公司管理员。
        /// </summary>
        CompanyAdministrator = 1 << 7,

        /// <summary>
        /// 分区管理员。
        /// </summary>
        AreaAdministrator = 1 << 8,

        /// <summary>
        /// 集团管理员。
        /// </summary>
        GroupAdministrator = 1 << 9,

        #endregion

        #region 系统
        /// <summary>
        /// 开发者。
        /// </summary>
        Developer = 1 << 10,
        /// <summary>
        /// 维护人。
        /// </summary>
        Maintainer = 1 << 11,
        /// <summary>
        /// 拥有者。
        /// </summary>
        Owner = 1 << 12
        #endregion

    }
    /// <summary>
    /// 用户。
    /// </summary>
    public class User
    {
        /// <summary>
        /// 用户ID。
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 机构ID。
        /// </summary>
        public long OrgId { get; set; }

        /// <summary>
        /// 公司ID。
        /// </summary>
        public long CompanyId { get; set; }

        /// <summary>
        /// 账户。
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// 名称。
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 头像。
        /// </summary>
        public string UserAvatar { get; set; }

        /// <summary>
        /// 公司名称。
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// 手机号。
        /// </summary>
        public string Tel { get; set; }

        /// <summary>
        /// 邮箱。
        /// </summary>
        public string Mail { get; set; }

        /// <summary>
        /// 角色。
        /// </summary>
        public int Sex { get; set; }

        /// <summary>
        /// 角色。
        /// </summary>
        [Naming("roleenum")]
        public UserRoleEnum Role { get; set; }

        /// <summary>
        /// 角色。
        /// </summary>
        [Naming("role")]
        public string RoleString { get; set; }

        /// <summary>
        /// 状态。
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 时间戳（登录的时间戳，可作为单点登录依据）。
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 创建日期时间戳。
        /// </summary>
        public DateTime Registered { get; set; }

        /// <summary>
        /// 最后一次修改日期时间戳。
        /// </summary>
        public DateTime Modified { get; set; }
    }

    public class User2
    {
        /// <summary>
        /// 角色。
        /// </summary>
        [Naming("roleenum")]
        public int Role { get; set; }
    }
    public class CopyTest
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CopyToTest : CopyTest
    {
        public DateTime Date { get; set; }
    }

    public class CopyTo2Test : CopyToTest
    {
    }

    public class MapToTest
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public DateTime Date { get; set; }
    }

    public class T1
    {
        public int A { get; set; }

        public string B { get; set; }
    }

    public class T2
    {
        public string A { get; set; }

        public int B { get; set; }
    }

    /// <summary>
    /// 更多服务列表。
    /// </summary>
    public class CommodityGroupListDto
    {
        /// <summary>
        /// 商品组名称。
        /// </summary>
        public string GroupName { get; set; }
        /// <summary>
        /// 商品组图标。
        /// </summary>
        public string GroupIcon { get; set; }
        /// <summary>
        /// 商品列表。
        /// </summary>
        public List<CommodityListDto> CommodityList { get; set; }
    }


    /// <summary>
    /// 更多服务商品组下商品列表。
    /// </summary>
    public class CommodityListDto
    {
        /// <summary>
        /// 商品Id。
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 商品名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 商品图标。
        /// </summary>
        public string Icon { get; set; }
        /// <summary>
        /// 商品描述。
        /// </summary>
        public string Desc { get; set; }
    }

    public class NonPublicConstructor
    {
        private NonPublicConstructor() { }
    }

    public class HasArgumentsConstructor
    {
        private readonly int i;

        public HasArgumentsConstructor(int i)
        {
            this.i = i;
        }
    }

    /// <summary>
    /// 发票DTO。
    /// </summary>
    public class ApplyDto
    {
        /// <summary>
        /// 商铺ID。
        /// </summary>
        public ulong ShopId { get; set; }
    }

    [TestClass]
    public class ObjectExtentions
    {
        public void Cast()
        {
            V();
        }

        public void V()
        {
            Cast();
        }

        [TestInitialize]
        public void Init()
        {
            //RuntimeServPools.TryAddSingleton<IMapper, CastingMapper>();

            using (var startup = new XStartup())
            {
                startup.DoStartup();
            }
        }

        [TestMethod]
        public void CastTo()
        {
            for (int i = 0; i < 1000; i++)
            {
                var guid = Mapper.Cast<Guid?>("0bbd0503-4879-42de-8cf0-666537b642e2");

                var list = new List<string> { "11111", "2111", "3111" };

                var stack = Mapper.Cast<Stack<string>>(list);

                var listInt = Mapper.Cast<List<int>>(list);

                var quene = Mapper.Cast<Queue<int>>(list);

                var queneGuid = Mapper.Cast<Queue<Guid>>(list);
            }
        }

        [TestMethod]
        public void CopyTo()
        {
            var copyTo = new CopyToExpression();

            //? 为类型“CopyTest”指定代理。
            copyTo.Use((profile, type) =>
            {
                if (type == typeof(CopyToTest))
                {
                    return source =>
                    {
                        var copy = (CopyTest)source;

                        return new CopyTest
                        {
                            Id = copy.Id,
                            Name = copy.Name
                        };
                    };
                }
                return profile.Create<CopyTest>(type);
            });

            RuntimeServPools.TryAddSingleton<ICopyToExpression>(() => copyTo);

            var value = new CopyToTest
            {
                Id = 1000,
                Name = "test",
                Date = DateTime.Now
            };

            var value2 = new CopyTo2Test
            {
                Id = 1000,
                Name = "test",
                Date = DateTime.Now
            };

            for (int i = 0; i < 100000; i++)
            {
                var copy1 = Mapper.Copy(value);

                value.Name = "test1";

                var copy2 = Mapper.Copy<CopyTest>(value);
                var copy3 = Mapper.Copy<CopyTest>(value2);

                value.Name = "test5";
            }

        }

        [TestMethod]
        public void MapTo()
        {
            //RuntimeServicePools.TryAdd<IProfileConfiguration, ProfileConfiguration>();
            var mapTo = RuntimeServPools.Singleton<IMapToExpression, MapToExpression>();

            //? 为类型“CopyTest”指定代理。
            mapTo.Run<CopyToTest, MapToTest>(source =>
            {
                return new MapToTest
                {
                    Id = source.Id,
                    Name = source.Name,
                    Date = source.Date
                };
            });

            RuntimeServPools.TryAddSingleton(() => mapTo);

            var t1 = new T1
            {
                A = 100,
                B = "10000"
            };

            var dic = new Dictionary<string, object>
            {
                ["sex"] = 1,
                ["roleenum"] = 32
            };

            var user = Mapper.Map<User>(dic);

            var user2 = new User2 { Role = 32 };

            var user3 = Mapper.Map<User>(user2);

            var user4 = Mapper.Map<User2>(user3);

            var t2 = Mapper.Map<T2>(t1);

            var value = new CopyToTest
            {
                Id = 1000,
                Name = "test",
                Date = DateTime.Now
            };

            for (int i = 0; i < 100000; i++)
            {
                //mapTo.MapTo<CopyTest>(value);
                var map1 = Mapper.Map<CopyTest>(value);

                value.Name = "test1";

                var map2 = Mapper.Map<MapToTest>(value);

                var map3 = Mapper.Map<IEnumerable<KeyValuePair<string, object>>>(value);

                var map4 = Mapper.Map<ICollection<KeyValuePair<string, object>>>(value);

                var map5 = Mapper.Map<IDictionary<string, object>>(value);

                var map6 = Mapper.Map<Dictionary<string, object>>(value);

                value.Name = "test5";
            }
        }

        [TestMethod]
        public void MapToList()
        {
            List<CommodityGroupListDto> pList = new List<CommodityGroupListDto>
            {
                new CommodityGroupListDto
                {
                    GroupIcon = "",
                    GroupName = "洗车分类",
                    CommodityList = new List<CommodityListDto>
                    {
                        new CommodityListDto
                        {
                            Id = "6e2b47e8d7b6c177c91b08d7ae3c35db",
                            Desc = "平台默认-包月洗车",
                            Icon = "http://api.eye56.com:9008/files/20200301/7e6dcaba-5c4f-475b-93d2-43c0b53b7126.png",
                            Name = "包月洗车"
                        }
                    }
                },
                new CommodityGroupListDto
                {
                    GroupIcon = "",
                    GroupName = "洗车分类",
                    CommodityList = new List<CommodityListDto>
                    {
                        new CommodityListDto
                        {
                            Id = "6e2b47e8d7b6c177c91b08d7ae3c35db",
                            Desc = "平台默认-包月洗车",
                            Icon = "http://api.eye56.com:9008/files/20200301/7e6dcaba-5c4f-475b-93d2-43c0b53b7126.png",
                            Name = "包月洗车"
                        },
                        new CommodityListDto
                        {
                            Id = "6e2b47e8d7b6c177c91b08d7ae3c35db",
                            Desc = "平台默认-包月洗车",
                            Icon = "http://api.eye56.com:9008/files/20200301/7e6dcaba-5c4f-475b-93d2-43c0b53b7126.png",
                            Name = "包月洗车"
                        },
                        new CommodityListDto
                        {
                            Id = "6e2b47e8d7b6c177c91b08d7ae3c35db",
                            Desc = "平台默认-包月洗车",
                            Icon = "http://api.eye56.com:9008",
                            Name = "包月洗车"
                        } } } };

            var x = Mapper.Copy(pList);
        }

        [TestMethod]
        public void MapDisposeTest()
        {
            var mapTo = RuntimeServPools.Singleton<IMapToExpression, MapToExpression>();

            //? 为类型“CopyTest”指定代理。
            mapTo.Run<CopyToTest, MapToTest>(source =>
            {
                return new MapToTest
                {
                    Id = source.Id,
                    Name = source.Name,
                    Date = source.Date
                };
            });

            RuntimeServPools.TryAddSingleton(() => mapTo);

            var t1 = new T1
            {
                A = 100,
                B = "10000"
            };

            var value = new CopyToTest
            {
                Id = 1000,
                Name = "test",
                Date = DateTime.Now
            };

            var t2 = Mapper.Map<T2>(t1);

            using (var map = new MapToExpression())
            {
                var t3 = map.Map<T2>(t1);

                var map1 = map.Map<MapToTest>(value);

                var map2 = Mapper.Map<MapToTest>(value);
            }

            var map3 = Mapper.Map<MapToTest>(value);
        }

        [TestMethod]
        public void CastTest()
        {
            var data = new
            {
                ShopId = "6651287474607755264"
            };

            var shopId = System.Convert.ChangeType("6651287474607755264", typeof(ulong));

            var value = Mapper.Map<ApplyDto>(data);
        }

        [TestMethod]
        public void MapTest2()
        {
            var dic = new Dictionary<string, object>
            {
                ["id"] = "12143",
                ["name"] = "测试",
                ["Icon"] = string.Empty
            };

            var commodity = Mapper.Map<CommodityListDto>(dic);
        }


        [TestMethod]
        public void MapTest3()
        {
            var testValue = new
            {
                Id = 0,
                name = string.Empty,
                icon = string.Empty
            };

            var dic = new Dictionary<string, object>
            {
                ["id"] = 12143,
                ["name"] = "测试",
                ["Icon"] = string.Empty
            };

            var commodity = Mapper.Map(dic, testValue.GetType());
        }

        public interface IEmptyA<T>
        {

        }

        public abstract class EmptyA<T> : IEmptyA<T>
        {

        }

        public class EmptyB<T> : EmptyA<T>
        {

        }

        public class EmptyC<T> : EmptyB<T>
        {

        }

        [TestMethod]
        public void EmptyTest()
        {
            Emptyable.Register(typeof(IEmptyA<>), typeof(EmptyB<>));//? 接口注册。
            Emptyable.Register(typeof(EmptyA<>), typeof(EmptyC<>)); //? 抽象类注册。

            var i = Emptyable.Empty<int>();
            var e = Emptyable.Empty<DateTimeKind>();
            var d = Emptyable.Empty<DateTime>();
            var s = Emptyable.Empty<string>();
            var c = Emptyable.Empty<CopyTest>();//? 默认构造函数。
            var nc = Emptyable.Empty<NonPublicConstructor>(); //? 私有构造函数。
            var na = Emptyable.Empty<HasArgumentsConstructor>(); //? 有参数构造函数。

            var ng = Emptyable.Empty<IEmptyA<int>>();
            var ng2 = Emptyable.Empty<EmptyA<string>>();
        }
    }
}
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeArts.Implements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeArts.Tests
{
    public class CopyTest
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CopyToTest : CopyTest
    {
        public DateTime Date { get; set; }
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
    /// 更多服务列表
    /// </summary>
    public class CommodityGroupListDto
    {
        /// <summary>
        /// 商品组名称
        /// </summary>
        public string GroupName { get; set; }
        /// <summary>
        /// 商品组图标
        /// </summary>
        public string GroupIcon { get; set; }
        /// <summary>
        /// 商品列表
        /// </summary>
        public List<CommodityListDto> CommodityList { get; set; }
    }


    /// <summary>
    /// 更多服务商品组下商品列表
    /// </summary>
    public class CommodityListDto
    {
        /// <summary>
        /// 商品Id
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 商品名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 商品图标
        /// </summary>
        public string Icon { get; set; }
        /// <summary>
        /// 商品描述
        /// </summary>
        public string Desc { get; set; }
    }

    public class NonPublicConstructor
    {
        private NonPublicConstructor() { }
    }

    public class NoArgumentsConstructor
    {
        public NoArgumentsConstructor(int i)
        {

        }
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

        [TestMethod]
        public void CastTo()
        {
            for (int i = 0; i < 1000; i++)
            {
                var guid = "0bbd0503-4879-42de-8cf0-666537b642e2".CastTo<Guid?>();

                var list = new List<string> { "11111", "2111", "3111" };

                var stack = list.CastTo<Stack<string>>();

                var listInt = list.CastTo<List<int>>();

                var quene = list.CastTo<Queue<int>>();

                var queneGuid = list.CastTo<Queue<Guid>>();
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

            RuntimeServManager.TryAddSingleton<ICopyToExpression>(() => copyTo);

            var value = new CopyToTest
            {
                Id = 1000,
                Name = "test",
                Date = DateTime.Now
            };

            for (int i = 0; i < 100000; i++)
            {
                var copy1 = value.CopyTo();

                value.Name = "test1";

                var copy2 = value.CopyTo<CopyTest>();

                value.Name = "test5";
            }
        }

        [TestMethod]
        public void MapTo()
        {
            //RuntimeServicePools.TryAdd<IProfileConfiguration, ProfileConfiguration>();
            var mapTo = RuntimeServManager.Singleton<IMapToExpression, MapToExpression>(x => { var y = x; });

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

            RuntimeServManager.TryAddSingleton(() => mapTo);

            var t1 = new T1
            {
                A = 100,
                B = "10000"
            };

            var t2 = t1.MapTo<T2>();

            var value = new CopyToTest
            {
                Id = 1000,
                Name = "test",
                Date = DateTime.Now
            };

            for (int i = 0; i < 100000; i++)
            {
                //mapTo.MapTo<CopyTest>(value);
                var map1 = value.MapTo<CopyTest>();

                value.Name = "test1";

                var map2 = value.MapTo<MapToTest>();

                var map3 = value.MapTo<IEnumerable<KeyValuePair<string, object>>>();

                var map4 = value.MapTo<ICollection<KeyValuePair<string, object>>>();

                var map5 = value.MapTo<IDictionary<string, object>>();

                var map6 = value.MapTo<Dictionary<string, object>>();

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


            var x = pList.CopyTo();

        }

        [TestMethod]
        public void MapDisposeTest()
        {
            var mapTo = RuntimeServManager.Singleton<IMapToExpression, MapToExpression>(x => { var y = x; });

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

            RuntimeServManager.TryAddSingleton(() => mapTo);

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

            var t2 = t1.MapTo<T2>();

            using (var map = new MapToExpression())
            {
                var t3 = map.Map<T2>(t1);

                var map1 = map.Map<MapToTest>(value);

                var map2 = value.MapTo<MapToTest>();
            }

            var map3 = value.MapTo<MapToTest>();
        }

        [TestMethod]
        public void EmptyTest()
        {
            var i = Emptyable.Empty<int>();
            var e = Emptyable.Empty<DateTimeKind>();
            var d = Emptyable.Empty<DateTime>();
            var s = Emptyable.Empty<string>();
            var c = Emptyable.Empty<CopyTest>();
            var nc = Emptyable.Empty<NonPublicConstructor>();
            var na = Emptyable.Empty<NoArgumentsConstructor>();
        }
    }
}
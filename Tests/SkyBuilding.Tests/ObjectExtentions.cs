using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkyBuilding.Implements;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SkyBuilding.Tests
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
            for (int i = 0; i < 100000; i++)
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

            RuntimeServicePools.TryAddSingleton<ICopyToExpression>(() => copyTo);

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

            var mapTo = RuntimeServicePools.Singleton<IMapToExpression, MapToExpression>();

            //? 为类型“CopyTest”指定代理。
            //mapTo.Run<CopyToTest, MapToTest>(source =>
            //{
            //    return new MapToTest
            //    {
            //        Id = source.Id,
            //        Name = source.Name,
            //        Date = source.Date
            //    };
            //});

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

                var map5 = value.MapTo<Dictionary<string, object>>();

                value.Name = "test5";
            }
        }
    }
}

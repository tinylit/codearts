using System;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CodeArts.Tests
{
    [TestClass]
    public class StringExtentions
    {
        [TestMethod]
        public void PropSugar()
        {
            var reg = new Regex(@"Bearer\s+(?<token>.+)");

            var grp = reg.Match("\"Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJBYnBVc2VySWQiOjE5MDQsIlByaWNlIjoyMjUwLjAsIkNvbXBhbnlOYW1lIjoiQklN6K6-6K6h56CU56m25Lit5b-DIiwiRGVwYXJ0bWVudElkIjoiSlpGWkpaRloxMDAxQTExMDAwMDAwMDAwMFpMTiIsIkRlcGFydG1lbnROYW1lIjoi6JGj5LqL5LyaIiwiRGVwYXJ0bWVudFBhdGgiOiIv6JGj5LqL5LyaIiwiRW1wbG95ZWVJZCI6ImxtYkxtRFAzUzZ5bzVhcHhFM0tKZ29EdmZlMD0iLCJVc2VySWQiOiJ6aGFvamlhanUiLCJVc2VyTmFtZSI6Iui1teWutumpuSIsIk1vYmlsZSI6IjEzOTgyMTA5MjMyIiwiUG9zaXRpb25JZCI6bnVsbCwiUG9zaXRpb25OYW1lIjpudWxsLCJTcGVjaWFsdHlOYW1lIjoi55S15rCUIiwiU3BlY2lhbHR5Tm8iOm51bGwsIkpvYkdyYWRlIjoi6aaW5bitMue6p0MiLCJTY29wZSI6bnVsbCwiSm9iR3JhZGVWYWx1ZSI6MCwiRXhwIjoxNTk3MzA5NjU0LCJKdGkiOm51bGwsInVzZXJfbmFtZSI6bnVsbCwiY2xpZW50X2lkIjpudWxsfQ.kuFLp4VdBXIIsiWaen2O478Oe7NOr8Yem29p-rgceu8");

            var token = grp.Groups["token"].Value;

            for (int i = 0; i < 100000; i++)
            {
                string value = $"{i}x{{z + z}}xx{{x ?? z}}-{{y?+z}}-{{z}}--{{xyz+sb}}-{{sb}}-{{abc}}".PropSugar(new { x = DateTimeKind.Utc, y = DateTime.Now, z = (string)null, xyz = new int[] { 1, 2, 3 }, sb = new StringBuilder("sb") }, new JsonSettings(NamingType.Normal));
            }
        }
    }
}

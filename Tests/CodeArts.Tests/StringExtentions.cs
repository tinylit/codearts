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
            for (int i = 0; i < 100000; i++)
            {
                string value = $"{i}x{{z + 100}}xx{{x ?? z + y}}-{{y?+z}}-{{z}}--{{xyz+sb}}-{{sb}}-{{abc}}".PropSugar(new { x = DateTimeKind.Utc, y = DateTime.Now, z = (string)null, xyz = new int[] { 1, 2, 3 }, sb = new StringBuilder("sb") }, new JsonSettings(NamingType.Normal));
            }
        }
    }
}

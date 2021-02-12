using Microsoft.VisualStudio.TestTools.UnitTesting;
using me.cqp.luohuaming.Setu.Code.OrderFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace me.cqp.luohuaming.Setu.Code.OrderFunctions.Tests
{
    [TestClass()]
    public class YandeReTagSearchTests
    {
        [TestMethod()]
        public void TagSearchTest()
        {
            var c = YandeReTagSearch.TagSearch("genshin impact");
            //Debug.WriteLine(c.ToString());
            Assert.AreNotEqual(c, null);
        }
    }
}
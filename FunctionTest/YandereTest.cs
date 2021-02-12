using System;
using System.Diagnostics;
using me.cqp.luohuaming.Setu.Code.OrderFunctions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionTest
{
    [TestClass]
    public class YandereTest
    {
        [TestMethod]
        public void GetYandePicTest()
        {
            var c = YandeRePic.GetYandePic(738696);
            Debug.WriteLine(c.ToString());
            Assert.AreEqual(c.PicLargeURL, "https://files.yande.re/jpeg/e90e4083a9a3b76edd09159f8b3e2068/yande.re%20738696%20elf%20enjo_kouhai%20iris_tia_aederlinde%20naked%20nipples%20pointy_ears%20takunomi.jpg");
        }
    }
}

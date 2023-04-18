using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Zvt.Parsers;

namespace Portalum.Zvt.UnitTest
{
    [TestClass]
    public class BmpParserTest
    {
        [TestMethod]
        public void GetDataLengthLLL_ValidData1_Successful()
        {
            var data = new byte[] { 0xF0, 0xF1, 0xF9 };

            var length = BmpParser.GetDataLengthLLL(data);

            Assert.AreEqual(19, length);
        }

        [TestMethod]
        public void GetDataLengthLLL_ValidData2_Successful()
        {
            var data = new byte[] { 0xF1, 0xF8, 0xF9 };

            var length = BmpParser.GetDataLengthLLL(data);

            Assert.AreEqual(189, length);
        }

        [TestMethod]
        public void GetDataLengthLL_ValidData1_Successful()
        {
            var data = new byte[] { 0xF1, 0xF8 };

            var length = BmpParser.GetDataLengthLL(data);

            Assert.AreEqual(18, length);
        }

        [TestMethod]
        public void GetDataLengthLL_ValidData2_Successful()
        {
            var data = new byte[] { 0xF1, 0xF0 };

            var length = BmpParser.GetDataLengthLL(data);

            Assert.AreEqual(10, length);
        }
    }
}

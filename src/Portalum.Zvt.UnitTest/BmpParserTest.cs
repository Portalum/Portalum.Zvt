using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Zvt.Parsers;
using Portalum.Zvt.Repositories;
using System.Text;

namespace Portalum.Zvt.UnitTest
{
    [TestClass]
    public class BmpParserTest
    {
        private BmpParser GetBmpParser()
        {
            IErrorMessageRepository errorMessageRepository = new EnglishErrorMessageRepository();

            var logger = LoggerHelper.GetLogger();
            var tlvParser = new TlvParser(logger.Object);
            var encoding = Encoding.GetEncoding(437);

            return new BmpParser(logger.Object, encoding, errorMessageRepository, tlvParser);
        }

        [TestMethod]
        public void GetDataLengthLLL_ValidData1_Successful()
        {
            var data = new byte[] { 0xF0, 0xF1, 0xF9 };

            var bmpParser = this.GetBmpParser();
            var length = bmpParser.GetDataLengthLLL(data);

            Assert.AreEqual(19, length);
        }

        [TestMethod]
        public void GetDataLengthLLL_ValidData2_Successful()
        {
            var data = new byte[] { 0xF1, 0xF8, 0xF9 };

            var bmpParser = this.GetBmpParser();
            var length = bmpParser.GetDataLengthLLL(data);

            Assert.AreEqual(189, length);
        }

        [TestMethod]
        public void GetDataLengthLL_ValidData1_Successful()
        {
            var data = new byte[] { 0xF1, 0xF8 };

            var bmpParser = this.GetBmpParser();
            var length = bmpParser.GetDataLengthLL(data);

            Assert.AreEqual(18, length);
        }

        [TestMethod]
        public void GetDataLengthLL_ValidData2_Successful()
        {
            var data = new byte[] { 0xF1, 0xF0 };

            var bmpParser = this.GetBmpParser();
            var length = bmpParser.GetDataLengthLL(data);

            Assert.AreEqual(10, length);
        }
    }
}

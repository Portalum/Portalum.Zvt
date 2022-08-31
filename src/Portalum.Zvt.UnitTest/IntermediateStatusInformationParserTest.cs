using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Zvt.Parsers;
using Portalum.Zvt.Repositories;
using System.Text;

namespace Portalum.Zvt.UnitTest
{
    [TestClass]
    public class IntermediateStatusInformationParserTest
    {
        private IntermediateStatusInformationParser GetIntermediateStatusInformationParser()
        {
            var logger = LoggerHelper.GetLogger();
            var encoding = Encoding.GetEncoding(437);

            IIntermediateStatusRepository intermediateStatusRepository = new EnglishIntermediateStatusRepository();
            IErrorMessageRepository errorMessageRepository = new EnglishErrorMessageRepository();

            return new IntermediateStatusInformationParser(logger.Object, encoding, intermediateStatusRepository, errorMessageRepository);
        }

        [TestMethod]
        public void GetMessage_InsertCard_Successful()
        {
            var parser = this.GetIntermediateStatusInformationParser();
            var statusMessage = parser.GetMessage(new byte[] { 0x0A });

            Assert.AreEqual("Insert card", statusMessage);
        }

        [TestMethod]
        public void GetMessage_EmptyData_Failure()
        {
            var parser = this.GetIntermediateStatusInformationParser();
            var statusMessage = parser.GetMessage(new byte[0]);

            Assert.IsNull(statusMessage);
        }

        [TestMethod]
        public void GetMessage_NullInput_Failure()
        {
            var parser = this.GetIntermediateStatusInformationParser();
            var statusMessage = parser.GetMessage(null);

            Assert.IsNull(statusMessage);
        }

        [TestMethod]
        public void GetMessage_TlvData_Failure()
        {
            var parser = this.GetIntermediateStatusInformationParser();
            var statusMessage = parser.GetMessage(new byte[] { 0xFF, 0x10, 0x06, 0x1A, 0x24, 0x18, 0x07, 0x08, 0x45, 0x55, 0x52, 0x20, 0x31, 0x2E, 0x32, 0x33, 0x07, 0x0C, 0x42, 0x69, 0x74, 0x74, 0x65, 0x20, 0x77, 0x61, 0x72, 0x74, 0x65, 0x6E });
            
            var expectedMessage = new StringBuilder();
            expectedMessage.AppendLine("EUR 1.23");
            expectedMessage.AppendLine("Bitte warten");
            
            Assert.AreEqual(expectedMessage.ToString(), statusMessage);
        }
    }
}

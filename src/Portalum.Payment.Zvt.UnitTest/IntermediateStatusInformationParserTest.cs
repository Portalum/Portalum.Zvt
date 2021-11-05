using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Payment.Zvt.Parsers;
using Portalum.Payment.Zvt.Repositories;

namespace Portalum.Payment.Zvt.UnitTest
{
    [TestClass]
    public class IntermediateStatusInformationParserTest
    {
        private IntermediateStatusInformationParser GetIntermediateStatusInformationParser()
        {
            var logger = LoggerHelper.GetLogger();
            IIntermediateStatusRepository intermediateStatusRepository = new EnglishIntermediateStatusRepository();

            return new IntermediateStatusInformationParser(logger.Object, intermediateStatusRepository);
        }

        [TestMethod]
        public void GetMessage_InsertCard_Successful()
        {
            var parser = this.GetIntermediateStatusInformationParser();
            var statusMessage = parser.GetMessage(new byte[] { 0x00, 0x00, 0x00, 0x0A });

            Assert.AreEqual("Insert card", statusMessage);
        }

        [TestMethod]
        public void GetMessage_InvalidData1_Failure()
        {
            var parser = this.GetIntermediateStatusInformationParser();
            var statusMessage = parser.GetMessage(new byte[] { 0x00, 0x00, 0x00 });

            Assert.IsNull(statusMessage);
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
    }
}

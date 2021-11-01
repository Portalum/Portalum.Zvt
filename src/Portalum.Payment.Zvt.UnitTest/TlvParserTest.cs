using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Payment.Zvt.Models;
using Portalum.Payment.Zvt.Parsers;

namespace Portalum.Payment.Zvt.UnitTest
{
    [TestClass]
    public class TlvParserTest
    {
        private TlvParser GetTlvParser()
        {
            var logger = LoggerHelper.GetLogger();
            return new TlvParser(logger.Object);
        }

        [TestMethod]
        public void GetTagFieldInfo_UniversalClassPrimitive1_Successful()
        {
            var tlvParser = this.GetTlvParser();

            var tagFieldInfo = tlvParser.GetTagFieldInfo(new byte[] { 0x1F, 0x07 });
            Assert.AreEqual(TlvTagFieldClassType.UniversalClass, tagFieldInfo.ClassType);
            Assert.AreEqual(TlvTagFieldDataObjectType.Primitive, tagFieldInfo.DataObjectType);
            Assert.AreEqual(7, tagFieldInfo.TagNumber);
            Assert.AreEqual(2, tagFieldInfo.NumberOfBytesThatCanBeSkipped);
            Assert.AreEqual("1F07", tagFieldInfo.Tag);
        }


        [TestMethod]
        public void GetTagFieldInfo_UniversalClassPrimitive2_Successful()
        {
            var tlvParser = this.GetTlvParser();

            var tagFieldInfo = tlvParser.GetTagFieldInfo(new byte[] { 0x1F, 0x26 });
            Assert.AreEqual(TlvTagFieldClassType.UniversalClass, tagFieldInfo.ClassType);
            Assert.AreEqual(TlvTagFieldDataObjectType.Primitive, tagFieldInfo.DataObjectType);
            Assert.AreEqual(38, tagFieldInfo.TagNumber);
            Assert.AreEqual(2, tagFieldInfo.NumberOfBytesThatCanBeSkipped);
            Assert.AreEqual("1F26", tagFieldInfo.Tag);
        }

        [TestMethod]
        public void GetTagFieldInfo_UniversalClassPrimitive3_Successful()
        {
            var tlvParser = this.GetTlvParser();

            var tagFieldInfo = tlvParser.GetTagFieldInfo(new byte[] { 0x1F, 0x80, 0x00 });
            Assert.AreEqual(TlvTagFieldClassType.UniversalClass, tagFieldInfo.ClassType);
            Assert.AreEqual(TlvTagFieldDataObjectType.Primitive, tagFieldInfo.DataObjectType);
            Assert.AreEqual(0, tagFieldInfo.TagNumber);
            Assert.AreEqual(3, tagFieldInfo.NumberOfBytesThatCanBeSkipped);
            Assert.AreEqual("1F8000", tagFieldInfo.Tag);
        }

        [TestMethod]
        public void GetTagFieldInfo_UniversalClassPrimitive4_Successful()
        {
            var tlvParser = this.GetTlvParser();

            var tagFieldInfo = tlvParser.GetTagFieldInfo(new byte[] { 0x07 });
            Assert.AreEqual(TlvTagFieldClassType.UniversalClass, tagFieldInfo.ClassType);
            Assert.AreEqual(TlvTagFieldDataObjectType.Primitive, tagFieldInfo.DataObjectType);
            Assert.AreEqual(7, tagFieldInfo.TagNumber);
            Assert.AreEqual(1, tagFieldInfo.NumberOfBytesThatCanBeSkipped);
            Assert.AreEqual("07", tagFieldInfo.Tag);
        }

        [TestMethod]
        public void GetTagFieldInfo_UniversalClassPrimitive5_Successful()
        {
            var tlvParser = this.GetTlvParser();

            var tagFieldInfo = tlvParser.GetTagFieldInfo(new byte[] { 0x09 });
            Assert.AreEqual(TlvTagFieldClassType.UniversalClass, tagFieldInfo.ClassType);
            Assert.AreEqual(TlvTagFieldDataObjectType.Primitive, tagFieldInfo.DataObjectType);
            Assert.AreEqual(9, tagFieldInfo.TagNumber);
            Assert.AreEqual(1, tagFieldInfo.NumberOfBytesThatCanBeSkipped);
            Assert.AreEqual("09", tagFieldInfo.Tag);
        }

        [TestMethod]
        public void GetTagFieldInfo_UniversalClassConstructed1_Successful()
        {
            var tlvParser = this.GetTlvParser();

            var tagFieldInfo = tlvParser.GetTagFieldInfo(new byte[] { 0x25 });
            Assert.AreEqual(TlvTagFieldClassType.UniversalClass, tagFieldInfo.ClassType);
            Assert.AreEqual(TlvTagFieldDataObjectType.Constructed, tagFieldInfo.DataObjectType);
            Assert.AreEqual(5, tagFieldInfo.TagNumber);
            Assert.AreEqual(1, tagFieldInfo.NumberOfBytesThatCanBeSkipped);
            Assert.AreEqual("25", tagFieldInfo.Tag);
        }

        [TestMethod]
        public void GetLength_3Bytes_Successful()
        {
            var tlvParser = this.GetTlvParser();

            var lengthInfo = tlvParser.GetLength(new byte[] { 0x82, 0x02, 0x8A });

            Assert.IsTrue(lengthInfo.Successful);
            Assert.AreEqual(650, lengthInfo.Length);
            Assert.AreEqual(3, lengthInfo.NumberOfBytesThatCanBeSkipped);
        }
    }
}

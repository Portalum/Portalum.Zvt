using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Payment.Zvt.Helpers;
using System;
using System.Linq;

namespace Portalum.Payment.Zvt.UnitTest
{
    [TestClass]
    public class ReceiveHandlerTest
    {
        [TestMethod]
        public void GetApduInfo_CorruptData1_Successful()
        {
            var data = new byte[] { 0x04, 0x0F };

            var apduInfo = ApduHelper.GetApduInfo(data);
            Assert.IsNull(apduInfo.ControlField);
            Assert.AreEqual(0, apduInfo.DataLength);
            Assert.AreEqual(0, apduInfo.DataStartIndex);
        }

        [TestMethod]
        public void GetApduInfo_CorruptData2_Successful()
        {
            var data = new byte[0];

            var apduInfo = ApduHelper.GetApduInfo(data);
            Assert.IsNull(apduInfo.ControlField);
            Assert.AreEqual(0, apduInfo.DataLength);
            Assert.AreEqual(0, apduInfo.DataStartIndex);
        }

        [TestMethod]
        public void GetApduInfo_DefaultLengthField_Successful()
        {
            var data = new byte[] { 0x04, 0x0F, 0x01, 0x6C };

            var apduInfo = ApduHelper.GetApduInfo(data);
            Assert.IsTrue(apduInfo.ControlField.SequenceEqual(new byte[] { 0x04, 0x0F }));
            Assert.AreEqual(1, apduInfo.DataLength);

            var apduData = data.AsSpan().Slice(apduInfo.DataStartIndex);
            Assert.AreEqual(0x6C, apduData[0]);
        }

        [TestMethod]
        public void GetApduInfo_ExtendedLengthField1_Successful()
        {
            var data = new byte[] { 0x04, 0x0F, 0xFF, 0x01, 0x00, 0x6C };

            var apduInfo = ApduHelper.GetApduInfo(data);
            Assert.IsTrue(apduInfo.ControlField.SequenceEqual(new byte[] { 0x04, 0x0F }));
            Assert.AreEqual(1, apduInfo.DataLength);

            var apduData = data.AsSpan().Slice(apduInfo.DataStartIndex);
            Assert.AreEqual(0x6C, apduData[0]);
        }

        [TestMethod]
        public void GetApduInfo_ExtendedLengthField2_Successful()
        {
            var data = new byte[] { 0x04, 0x0F, 0xFF, 0x00, 0x01, 0x6C };

            var apduInfo = ApduHelper.GetApduInfo(data);
            Assert.IsTrue(apduInfo.ControlField.SequenceEqual(new byte[] { 0x04, 0x0F }));
            Assert.AreEqual(256, apduInfo.DataLength);

            var apduData = data.AsSpan().Slice(apduInfo.DataStartIndex);
            Assert.AreEqual(0x6C, apduData[0]);
        }

        [TestMethod]
        public void GetApduInfo_CorruptLength_Successful()
        {
            var data = new byte[] { 0x04, 0x0F, 0x06, 0x00, 0x01, 0x02, 0x03 };

            var apduInfo = ApduHelper.GetApduInfo(data);
            Assert.IsTrue(apduInfo.ControlField.SequenceEqual(new byte[] { 0x04, 0x0F }));
            Assert.AreEqual(6, apduInfo.DataLength);
        }
    }
}

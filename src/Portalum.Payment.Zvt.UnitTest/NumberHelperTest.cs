using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Payment.Zvt.Helpers;
using System;
using System.Linq;

namespace Portalum.Payment.Zvt.UnitTest
{
    [TestClass]
    public class NumberHelperTest
    {
        [TestMethod]
        public void DecimalToBcd_CommercialRoundOff_Successful()
        {
            var amount = 20.203M;
            var expected = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x20, 0x20 };

            var result = NumberHelper.DecimalToBcd(amount);
            Assert.IsTrue(result.SequenceEqual(expected));
        }

        [TestMethod]
        public void DecimalToBcd_CommercialRoundUp_Successful()
        {
            var amount = 20.206M;
            var expected = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x20, 0x21 };

            var result = NumberHelper.DecimalToBcd(amount);
            Assert.IsTrue(result.SequenceEqual(expected));
        }

        [TestMethod]
        public void DecimalToBcd_MaximumNumberOfDigits_Successful()
        {
            var amount = 1_234_567_890M;
            var expected = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0x00 };

            var result = NumberHelper.DecimalToBcd(amount);
            Assert.IsTrue(result.SequenceEqual(expected));
        }

        [TestMethod]
        public void DecimalToBcd_MaximumNumberOfDigits1_Successful()
        {
            var amount = 91_234_567_890M;
            var expected = new byte[0];

            var result = NumberHelper.DecimalToBcd(amount);
            Assert.IsTrue(result.SequenceEqual(expected));
        }

        [TestMethod]
        public void IntToBcd_6DigitNumber_Successful()
        {
            var number = 123456;
            var expected = new byte[] { 0x12, 0x34, 0x56 };

            var result = NumberHelper.IntToBcd(number, 3);
            Assert.IsTrue(result.SequenceEqual(expected));
        }

        [TestMethod]
        public void IntToBcd_4DigitNumber_Successful()
        {
            var number = 1234;
            var expected = new byte[] { 0x00, 0x12, 0x34 };

            var result = NumberHelper.IntToBcd(number, 3);
            Assert.IsTrue(result.SequenceEqual(expected));
        }

        [TestMethod]
        public void IntToBcd_8DigitNumber_Successful()
        {
            var number = 12345678;
            var expected = new byte[] { 0x34, 0x56, 0x78 };

            var result = NumberHelper.IntToBcd(number, 3);
            Assert.IsTrue(result.SequenceEqual(expected));
        }

        [TestMethod]
        public void IntToBcd_EuroCurrencyNumericCode_Successful()
        {
            var number = 978;
            var expected = new byte[] { 0x09, 0x78 };

            var result = NumberHelper.IntToBcd(number, 2);
            Assert.IsTrue(result.SequenceEqual(expected));
        }
    }
}

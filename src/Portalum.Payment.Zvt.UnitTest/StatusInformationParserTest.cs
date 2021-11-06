using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Payment.Zvt.Helpers;
using Portalum.Payment.Zvt.Parsers;
using Portalum.Payment.Zvt.Repositories;
using System;

namespace Portalum.Payment.Zvt.UnitTest
{
    [TestClass]
    public class StatusInformationParserTest
    {
        private StatusInformationParser GetStatusInformationParser()
        {
            var logger = LoggerHelper.GetLogger();
            IErrorMessageRepository errorMessageRepository = new EnglishErrorMessageRepository();

            return new StatusInformationParser(logger.Object, errorMessageRepository);
        }

        [TestMethod]
        public void Parse_AbortByKey_Successful()
        {
            var byteData = new byte[] { 0x04, 0x0F, 0x1E, 0x27, 0x6C, 0x29, 0x28, 0x00, 0x48, 0x69, 0x3C, 0xF0, 0xF1, 0xF9, 0x41, 0x62, 0x62, 0x72, 0x75, 0x63, 0x68, 0x20, 0x64, 0x75, 0x72, 0x63, 0x68, 0x20, 0x4B, 0x75, 0x6E, 0x64, 0x65 };

            var apduInfo = ApduHelper.GetApduInfo(byteData);
            var data = byteData.AsSpan().Slice(apduInfo.DataStartIndex);

            var statusInformationParser = this.GetStatusInformationParser();
            var statusInformation = statusInformationParser.Parse(data);

            Assert.AreEqual("Abbruch durch Kunde", statusInformation.AdditionalText);
            Assert.AreEqual("abort via timeout or abort-key", statusInformation.ErrorMessage);
            Assert.AreEqual(671107177, statusInformation.TerminalIdentifier);
        }

        [TestMethod]
        public void Parse_SystemError_Successful()
        {
            var byteData = new byte[] { 0x04, 0x0F, 0x18, 0x27, 0xFF, 0x29, 0x28, 0x00, 0x48, 0x69, 0x3C, 0xF0, 0xF1, 0xF3, 0x42, 0x65, 0x74, 0x72, 0x61, 0x67, 0x20, 0x66, 0x61, 0x6C, 0x73, 0x63, 0x68 };

            var apduInfo = ApduHelper.GetApduInfo(byteData);
            var data = byteData.AsSpan().Slice(apduInfo.DataStartIndex);

            var statusInformationParser = this.GetStatusInformationParser();
            var statusInformation = statusInformationParser.Parse(data);

            Assert.AreEqual("Betrag falsch", statusInformation.AdditionalText);
            Assert.AreEqual("system error (= other/unknown error), See TLV tags 1F16 and 1F17", statusInformation.ErrorMessage);
            Assert.AreEqual(671107177, statusInformation.TerminalIdentifier);
        }

        [TestMethod]
        public void Parse_PaymentGood1_Successful()
        {
            var byteData = new byte[] { 0x04, 0x0F, 0x77, 0x27, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x20, 0x40, 0x0B, 0x00, 0x04, 0x70, 0x0C, 0x22, 0x39, 0x53, 0x0D, 0x10, 0x06, 0x0E, 0x25, 0x12, 0x17, 0x00, 0x01, 0x19, 0x70, 0x22, 0xF0, 0xF8, 0xEE, 0xEE, 0xEE, 0xEE, 0xEE, 0xEE, 0x47, 0x71, 0x29, 0x28, 0x00, 0x48, 0x69, 0x2A, 0x31, 0x30, 0x30, 0x34, 0x36, 0x31, 0x37, 0x36, 0x33, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x3B, 0x36, 0x31, 0x31, 0x38, 0x30, 0x34, 0x00, 0x00, 0x3C, 0xF0, 0xF1, 0xF4, 0x47, 0x45, 0x4E, 0x2E, 0x4E, 0x52, 0x2E, 0x3A, 0x36, 0x31, 0x31, 0x38, 0x30, 0x34, 0x49, 0x09, 0x78, 0x87, 0x04, 0x70, 0x88, 0x00, 0x04, 0x70, 0x8A, 0x2E, 0x8B, 0xF1, 0xF7, 0x44, 0x65, 0x62, 0x69, 0x74, 0x20, 0x4D, 0x61, 0x73, 0x74, 0x65, 0x72, 0x63, 0x61, 0x72, 0x64, 0x00 };

            var apduInfo = ApduHelper.GetApduInfo(byteData);
            var data = byteData.AsSpan().Slice(apduInfo.DataStartIndex);

            var statusInformationParser = this.GetStatusInformationParser();
            var statusInformation = statusInformationParser.Parse(data);

            Assert.AreEqual("GEN.NR.:611804", statusInformation.AdditionalText);
            Assert.AreEqual("Debit Mastercard", statusInformation.CardName);
            Assert.AreEqual(20.4M, statusInformation.Amount);
            Assert.AreEqual(978, statusInformation.CurrencyCode);
            Assert.AreEqual(new TimeSpan(22, 39, 53), statusInformation.Time);
            Assert.AreEqual(671107177, statusInformation.TerminalIdentifier);
        }

        [TestMethod]
        public void Parse_PaymentGood2_Successful()
        {
            var byteData = new byte[] { 0x04, 0x0F, 0x77, 0x27, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x20, 0x40, 0x0B, 0x00, 0x04, 0x63, 0x0C, 0x21, 0x04, 0x43, 0x0D, 0x10, 0x06, 0x0E, 0x25, 0x12, 0x17, 0x00, 0x01, 0x19, 0x70, 0x22, 0xF0, 0xF8, 0xEE, 0xEE, 0xEE, 0xEE, 0xEE, 0xEE, 0x47, 0x71, 0x29, 0x28, 0x00, 0x48, 0x69, 0x2A, 0x31, 0x30, 0x30, 0x34, 0x36, 0x31, 0x37, 0x36, 0x33, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x3B, 0x39, 0x37, 0x38, 0x35, 0x33, 0x39, 0x00, 0x00, 0x3C, 0xF0, 0xF1, 0xF4, 0x47, 0x45, 0x4E, 0x2E, 0x4E, 0x52, 0x2E, 0x3A, 0x39, 0x37, 0x38, 0x35, 0x33, 0x39, 0x49, 0x09, 0x78, 0x87, 0x04, 0x63, 0x88, 0x00, 0x04, 0x63, 0x8A, 0x2E, 0x8B, 0xF1, 0xF7, 0x44, 0x65, 0x62, 0x69, 0x74, 0x20, 0x4D, 0x61, 0x73, 0x74, 0x65, 0x72, 0x63, 0x61, 0x72, 0x64, 0x00 };

            var apduInfo = ApduHelper.GetApduInfo(byteData);
            var data = byteData.AsSpan().Slice(apduInfo.DataStartIndex);

            var statusInformationParser = this.GetStatusInformationParser();
            var statusInformation = statusInformationParser.Parse(data);

            Assert.AreEqual("GEN.NR.:978539", statusInformation.AdditionalText);
            Assert.AreEqual("Debit Mastercard", statusInformation.CardName);
            Assert.AreEqual(20.4M, statusInformation.Amount);
            Assert.AreEqual(671107177, statusInformation.TerminalIdentifier);
        }

        [TestMethod]
        public void Parse_TlvInfos1_Successful()
        {
            var dataHex = "04-0F-65-27-00-0C-23-13-55-0D-10-29-0E-99-99-22-F0-F8-EE-EE-EE-EE-EE-EE-47-71-A0-01-29-00-00-00-06-87-00-08-0B-00-00-00-8A-06-8B-F1-F1-4D-41-53-54-45-52-43-41-52-44-00-06-2E-2F-08-1F-12-01-02-1F-10-01-00-60-1B-43-07-A0-00-00-00-04-10-10-44-10-44-65-62-69-74-20-4D-61-73-74-65-72-63-61-72-64-1F-2B-04-10-01-50-08";
            var byteData = ByteHelper.HexToByteArray(dataHex);

            var apduInfo = ApduHelper.GetApduInfo(byteData);
            var data = byteData.AsSpan().Slice(apduInfo.DataStartIndex);

            var statusInformationParser = this.GetStatusInformationParser();
            var statusInformation = statusInformationParser.Parse(data);

            Assert.AreEqual("MASTERCARD", statusInformation.CardName);
            Assert.AreEqual(6, statusInformation.TerminalIdentifier);
            Assert.AreEqual("no error", statusInformation.ErrorMessage);
            Assert.AreEqual("NFC", statusInformation.CardTechnology);
            Assert.AreEqual("No Cardholder authentication", statusInformation.CardholderAuthentication);
        }

        [TestMethod]
        public void Parse_TlvInfos2_Successful()
        {
            var dataHex = "04-0F-6C-27-00-04-00-00-00-00-20-40-0C-21-09-16-0D-11-01-0E-99-99-22-F0-F8-EE-EE-EE-EE-EE-EE-47-71-A0-01-29-00-00-00-06-87-00-05-0B-00-00-00-8A-06-8B-F1-F1-4D-41-53-54-45-52-43-41-52-44-00-06-2E-2F-08-1F-12-01-01-1F-10-01-03-60-1B-43-07-A0-00-00-00-04-10-10-44-10-44-65-62-69-74-20-4D-61-73-74-65-72-63-61-72-64-1F-2B-04-10-01-60-05";
            var byteData = ByteHelper.HexToByteArray(dataHex);

            var apduInfo = ApduHelper.GetApduInfo(byteData);
            var data = byteData.AsSpan().Slice(apduInfo.DataStartIndex);

            var statusInformationParser = this.GetStatusInformationParser();
            var statusInformation = statusInformationParser.Parse(data);

            Assert.AreEqual("MASTERCARD", statusInformation.CardName);
            Assert.AreEqual(6, statusInformation.TerminalIdentifier);
            Assert.AreEqual("no error", statusInformation.ErrorMessage);
            Assert.AreEqual("Chip", statusInformation.CardTechnology);
            Assert.AreEqual("Offline encrypted Pin", statusInformation.CardholderAuthentication);
        }
    }
}

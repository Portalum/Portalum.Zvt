using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Payment.Zvt.Helpers;
using Portalum.Payment.Zvt.Models;
using Portalum.Payment.Zvt.Parsers;
using Portalum.Payment.Zvt.Repositories;
using System;
using System.Text;

namespace Portalum.Payment.Zvt.UnitTest
{
    [TestClass]
    public class PrintTextBlockTest
    {
        [AssemblyInitialize]
        public static void Init(TestContext testContext)
        {
            //Automatic register in ZvtClient, only required in this test
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private PrintTextBlockParser GetPrintTextBlockParser()
        {
            IErrorMessageRepository errorMessageRepository = new EnglishErrorMessageRepository();

            var logger = LoggerHelper.GetLogger();
            return new PrintTextBlockParser(logger.Object, errorMessageRepository);
        }

        [TestMethod]
        public void Parse_CardholderReceipt_Successful()
        {
            var hexData = "06-D3-FF-8E-02-06-82-02-8A-1F-07-01-02-25-82-02-82-07-18-20-20-4C-65-69-68-73-74-65-6C-6C-75-6E-67-20-76-6F-6E-20-68-6F-62-65-78-07-16-20-20-20-20-20-66-81-72-20-50-4F-52-54-41-4C-55-4D-20-47-6D-62-48-07-12-20-20-20-20-20-20-20-20-48-65-72-72-20-54-65-73-74-31-07-15-20-20-20-20-20-5A-56-54-20-54-43-50-2F-49-50-2C-20-44-48-43-50-07-1B-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-07-1B-31-39-2E-31-30-2E-32-30-32-31-20-20-20-20-20-20-20-20-20-31-33-3A-33-30-3A-30-34-07-1B-30-30-30-30-30-31-33-20-20-20-20-30-30-30-34-20-20-20-20-20-20-30-30-30-30-30-31-07-13-20-20-20-20-20-20-20-20-4B-55-4E-44-45-4E-42-45-4C-45-47-07-1B-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-07-12-54-65-72-6D-69-6E-61-6C-3A-20-41-48-30-30-30-30-30-36-07-11-42-65-6C-65-67-23-20-20-3A-20-48-30-30-34-30-30-31-07-12-4C-3A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-30-30-30-30-07-13-41-49-44-3A-20-41-30-30-30-30-30-30-30-30-34-31-30-31-30-07-11-4B-61-72-74-65-3A-20-4D-41-53-54-45-52-43-41-52-44-07-18-41-70-70-2E-20-20-3A-20-44-65-62-69-74-20-4D-61-73-74-65-72-63-61-72-64-07-13-20-20-20-20-20-20-20-20-43-6F-6E-74-61-63-74-6C-65-73-73-07-04-4B-41-55-46-07-01-20-07-05-53-55-4D-4D-45-07-12-20-20-20-20-20-20-20-20-20-45-55-52-3A-20-31-2C-30-30-07-01-20-07-1B-54-72-61-63-65-23-3A-20-20-20-20-20-20-20-20-20-20-20-20-31-30-30-30-34-30-30-31-07-01-20-07-09-52-65-66-65-72-65-6E-7A-3A-07-01-20-07-1A-20-41-75-74-6F-72-69-73-69-65-72-75-6E-67-73-63-6F-64-65-3A-33-36-37-36-35-35-07-11-20-20-20-20-20-20-20-20-20-28-52-43-20-30-30-31-29-07-14-20-20-20-20-47-65-6E-65-68-6D-69-67-74-20-33-36-37-36-35-35-07-10-20-20-20-20-20-20-20-30-30-31-30-30-34-30-30-31-07-01-20-07-19-20-41-56-3A-20-2B-30-34-2E-30-37-20-28-4A-75-6E-20-31-31-20-32-30-32-31-29-07-1B-54-49-3A-20-45-20-44-54-3A-20-30-2F-30-2F-20-4F-46-3A-20-30-2F-30-2F-20-43-47-3A-07-0F-20-20-20-20-20-20-20-20-20-20-20-31-2F-30-2F-07-01-20-07-01-20-07-01-20-07-01-20-07-01-20-09-01-81";
            var byteData = ByteHelper.HexToByteArray(hexData);
            var apduInfo = ApduHelper.GetApduInfo(byteData);
            var data = byteData.AsSpan().Slice(apduInfo.DataStartIndex);

            var parser = this.GetPrintTextBlockParser();
            var receiptInfo = parser.Parse(data);

            Assert.IsTrue(receiptInfo.CompletelyProcessed);
            Assert.AreEqual(ReceiptType.Cardholder, receiptInfo.ReceiptType);
            Assert.IsTrue(receiptInfo.Content.StartsWith("  Leihstellung von hobex"));
            Assert.IsTrue(receiptInfo.Content.Contains("KUNDENBELEG"));
        }

        [TestMethod]
        public void Parse_MerchantReceipt_Successful()
        {
            var hexData = "06-D3-FF-A4-02-06-82-02-A0-1F-07-01-01-25-82-02-98-07-18-20-20-4C-65-69-68-73-74-65-6C-6C-75-6E-67-20-76-6F-6E-20-68-6F-62-65-78-07-16-20-20-20-20-20-66-81-72-20-50-4F-52-54-41-4C-55-4D-20-47-6D-62-48-07-12-20-20-20-20-20-20-20-20-48-65-72-72-20-54-65-73-74-31-07-15-20-20-20-20-20-5A-56-54-20-54-43-50-2F-49-50-2C-20-44-48-43-50-07-1B-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-07-1B-31-39-2E-31-30-2E-32-30-32-31-20-20-20-20-20-20-20-20-20-31-33-3A-33-30-3A-30-34-07-1B-30-30-30-30-30-31-33-20-20-20-20-30-30-30-34-20-20-20-20-20-20-30-30-30-30-30-31-07-13-20-20-20-20-20-20-20-48-8E-4E-44-4C-45-52-42-45-4C-45-47-07-1B-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-07-12-54-65-72-6D-69-6E-61-6C-3A-20-41-48-30-30-30-30-30-36-07-11-42-65-6C-65-67-23-20-20-3A-20-48-30-30-34-30-30-31-07-12-4C-3A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-2A-30-30-30-30-07-13-41-49-44-3A-20-41-30-30-30-30-30-30-30-30-34-31-30-31-30-07-11-4B-61-72-74-65-3A-20-4D-41-53-54-45-52-43-41-52-44-07-18-41-70-70-2E-20-20-3A-20-44-65-62-69-74-20-4D-61-73-74-65-72-63-61-72-64-07-13-20-20-20-20-20-20-20-20-43-6F-6E-74-61-63-74-6C-65-73-73-07-04-4B-41-55-46-07-01-20-07-05-53-55-4D-4D-45-07-12-20-20-20-20-20-20-20-20-20-45-55-52-3A-20-31-2C-30-30-07-01-20-07-1B-54-72-61-63-65-23-3A-20-20-20-20-20-20-20-20-20-20-20-20-31-30-30-30-34-30-30-31-07-01-20-07-09-52-65-66-65-72-65-6E-7A-3A-07-01-20-07-1A-20-41-75-74-6F-72-69-73-69-65-72-75-6E-67-73-63-6F-64-65-3A-33-36-37-36-35-35-07-11-20-20-20-20-20-20-20-20-20-28-52-43-20-30-30-31-29-07-14-20-20-20-20-47-65-6E-65-68-6D-69-67-74-20-33-36-37-36-35-35-07-10-20-20-20-20-20-20-20-30-30-31-30-30-34-30-30-31-07-01-20-07-11-20-20-20-20-20-20-20-20-20-4B-45-49-4E-20-43-56-4D-07-01-20-07-19-20-41-56-3A-20-2B-30-34-2E-30-37-20-28-4A-75-6E-20-31-31-20-32-30-32-31-29-07-1B-54-49-3A-20-45-20-44-54-3A-20-30-2F-30-2F-20-4F-46-3A-20-30-2F-30-2F-20-43-47-3A-07-0F-20-20-20-20-20-20-20-20-20-20-20-31-2F-30-2F-07-01-20-07-01-20-07-01-20-07-01-20-07-01-20-09-01-81";
            var byteData = ByteHelper.HexToByteArray(hexData);
            var apduInfo = ApduHelper.GetApduInfo(byteData);
            var data = byteData.AsSpan().Slice(apduInfo.DataStartIndex);

            var parser = this.GetPrintTextBlockParser();
            var receiptInfo = parser.Parse(data);

            Assert.IsTrue(receiptInfo.CompletelyProcessed);
            Assert.AreEqual(ReceiptType.Merchant, receiptInfo.ReceiptType);
            Assert.IsTrue(receiptInfo.Content.StartsWith("  Leihstellung von hobex"));
            Assert.IsTrue(receiptInfo.Content.Contains("HÄNDLERBELEG"));
        }

        [TestMethod]
        public void Parse_AdministrationReceipt_Successful()
        {
            var hexData = "06-D3-AC-06-81-A9-1F-07-01-03-25-81-A2-07-1B-30-32-2E-31-31-2E-32-30-32-31-20-20-20-20-20-20-20-20-20-31-37-3A-30-35-3A-30-32-07-0C-54-49-44-3A-41-48-30-30-30-30-30-36-07-14-41-62-73-63-68-6C-75-73-73-20-2D-20-6B-65-69-6E-65-20-54-58-07-19-20-41-56-3A-20-2B-30-34-2E-30-37-20-28-4A-75-6E-20-31-31-20-32-30-32-31-29-07-1B-54-49-3A-20-45-20-44-54-3A-20-30-2F-30-2F-20-4F-46-3A-20-30-2F-30-2F-20-43-47-3A-07-0F-20-20-20-20-20-20-20-20-20-20-20-31-2F-30-2F-07-01-20-07-01-20-07-01-20-07-01-20-07-01-20-07-01-20-07-01-20-09-01-81";
            var byteData = ByteHelper.HexToByteArray(hexData);
            var apduInfo = ApduHelper.GetApduInfo(byteData);
            var data = byteData.AsSpan().Slice(apduInfo.DataStartIndex);

            var parser = this.GetPrintTextBlockParser();
            var receiptInfo = parser.Parse(data);

            Assert.IsTrue(receiptInfo.CompletelyProcessed);
            Assert.AreEqual(ReceiptType.Administration, receiptInfo.ReceiptType);
            Assert.IsTrue(receiptInfo.Content.Contains("Abschluss - keine TX"));
        }
    }
}

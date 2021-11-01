using Microsoft.VisualStudio.TestTools.UnitTesting;
using Portalum.Payment.Zvt.Models;
using Portalum.Payment.Zvt.Repositories;
using System.Diagnostics;

namespace Portalum.Payment.Zvt.UnitTest
{
    [TestClass]
    public class PrintLineTest
    {
        private ReceiveHandler GetReceiveHandler()
        {
            IErrorMessageRepository errorMessageRepository = new EnglishErrorMessageRepository();

            var logger = LoggerHelper.GetLogger();
            return new ReceiveHandler(logger.Object, errorMessageRepository);
        }

        [TestMethod]
        public void ProcessData_LineReceived1_Successful()
        {
            var hexLines = new string[]
            {
                "06-D1-14-40-2A-2A-20-4B-41-53-53-45-4E-53-43-48-4E-49-54-54-20-2A-2A",
                "06-D1-0E-40-54-45-53-54-20-50-6F-72-74-61-6C-75-6D",
                "06-D1-09-40-52-49-45-44-47-20-35-30",
                "06-D1-0E-40-36-38-35-30-20-44-4F-52-4E-42-49-52-4E",
                "06-D1-01-40",
                "06-D1-01-40",
                "06-D1-02-00-20",
                "06-D1-19-00-54-65-72-6D-69-6E-61-6C-2D-49-44-3A-20-20-20-20-32-38-30-30-34-38-36-39",
                "06-D1-14-00-31-38-2E-31-30-2E-32-30-32-31-20-32-32-3A-35-35-3A-35-30",
                "06-D1-02-00-20",
                "06-D1-19-00-20-20-20-20-20-20-20-20-41-6E-7A-61-68-6C-20-20-20-20-20-20-20-45-55-52",
                "06-D1-17-00-56-69-73-61-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20",
                "06-D1-19-00-20-20-20-20-20-20-20-20-20-20-20-20-20-30-20-20-20-20-20-20-30-2C-30-30",
                "06-D1-17-00-56-20-50-41-59-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20",
                "06-D1-19-00-20-20-20-20-20-20-20-20-20-20-20-20-20-30-20-20-20-20-20-20-30-2C-30-30",
                "06-D1-17-00-4D-61-73-74-65-72-63-61-72-64-20-20-20-20-20-20-20-20-20-20-20-20",
                "06-D1-19-00-20-20-20-20-20-20-20-20-20-20-20-20-20-30-20-20-20-20-20-20-30-2C-30-30",
                "06-D1-17-00-4D-61-65-73-74-72-6F-2F-44-4D-43-20-41-54-20-20-20-20-20-20-20-20",
                "06-D1-19-00-20-20-20-20-20-20-20-20-20-20-20-20-20-30-20-20-20-20-20-20-30-2C-30-30",
                "06-D1-17-00-2D-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20",
                "06-D1-19-00-20-20-20-20-20-20-20-20-20-20-20-20-20-30-20-20-20-20-20-20-30-2C-30-30",
                "06-D1-19-40-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D",
                "06-D1-19-00-47-65-73-61-6D-74-20-20-20-20-20-20-20-30-20-20-20-20-20-20-30-2C-30-30",
                "06-D1-02-00-20",
                "06-D1-19-40-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D-2D",
                "06-D1-19-00-56-69-73-61-20-20-20-20-20-20-20-20-20-20-20-20-23-3A-30-30-30-34-39-39",
                "06-D1-19-00-56-55-3A-20-20-20-20-20-20-20-20-20-20-20-20-31-30-30-34-36-31-37-36-33",
                "06-D1-19-00-56-20-50-41-59-20-20-20-20-20-20-20-20-20-20-20-23-3A-30-30-30-34-39-39",
                "06-D1-19-00-56-55-3A-20-20-20-20-20-20-20-20-20-20-20-20-31-30-30-34-36-31-37-36-33",
                "06-D1-19-00-4D-61-73-74-65-72-63-61-72-64-20-20-20-20-20-20-23-3A-30-30-30-34-39-39",
                "06-D1-19-00-56-55-3A-20-20-20-20-20-20-20-20-20-20-20-20-31-30-30-34-36-31-37-36-33",
                "06-D1-19-00-4D-61-65-73-74-72-6F-2F-44-4D-43-20-41-54-20-20-23-3A-30-30-30-34-39-39",
                "06-D1-19-00-56-55-3A-20-20-20-20-20-20-20-20-20-20-20-20-31-30-30-34-36-31-37-36-33",
                "06-D1-19-00-2D-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-23-3A-30-30-30-34-39-39",
                "06-D1-19-00-56-55-3A-20-20-20-20-20-20-20-20-20-20-20-20-31-30-30-34-36-31-37-36-33",
                "06-D1-19-40-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D-3D",
                "06-D1-02-00-20",
                "06-D1-02-00-20",
                "06-D1-02-00-20",
                "06-D1-02-00-20",
                "06-D1-01-81",
                "06-0F-02-27-E0"
            };

            var receiveHandler = this.GetReceiveHandler();
            receiveHandler.LineReceived += ReceiveHandlerLineReceived;

            foreach (var hexLine in hexLines)
            {
                var data = ByteHelper.HexToByteArray(hexLine);
                receiveHandler.ProcessData(data);
            }

            receiveHandler.LineReceived -= ReceiveHandlerLineReceived;
        }

        [TestMethod]
        public void ProcessData_LineReceived2_Successful()
        {
            var hexLines = new string[]
            {
                "06-D1-10-40-2A-2A-20-4B-41-53-53-45-4E-53-43-48-4E-49-54-54-20-2A-2A", //Invalid length, data to large
                "06-D1-18-40-2A-2A-20-4B-41-53-53-45-4E-53-43-48-4E-49-54-54-20-2A-2A", //Invalid length, data too short
            };

            var receiveHandler = this.GetReceiveHandler();
            receiveHandler.LineReceived += ReceiveHandlerLineReceived;

            foreach (var hexLine in hexLines)
            {
                var data = ByteHelper.HexToByteArray(hexLine);
                if (receiveHandler.ProcessData(data))
                {
                    Assert.Fail("Corrupt data");
                }
            }

            receiveHandler.LineReceived -= ReceiveHandlerLineReceived;
        }

        private void ReceiveHandlerLineReceived(PrintLineInfo printLineInfo)
        {
            Debug.WriteLine(printLineInfo.Text);
        }
    }
}

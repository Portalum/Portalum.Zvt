using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Portalum.Zvt.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Portalum.Zvt.UnitTest
{
    [TestClass]
    public class ZvtCommunicationTest
    {
        [TestMethod]
        public async Task SendCommandAsync_AcknowledgeReceived_Successful()
        {
            var additionalDataReceived = false;

            ProcessData dataReceived(byte[] data)
            {
                additionalDataReceived = true;
                return new ProcessData{ State = ProcessDataState.Processed };
            }

            var loggerZvtCommunication = LoggerHelper.GetLogger<ZvtCommunication>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var zvtCommunication = new ZvtCommunication(loggerZvtCommunication.Object, mockDeviceCommunication.Object);
            zvtCommunication.DataReceived += dataReceived;

            var sendCommandTask = zvtCommunication.SendCommandAsync(new byte[] { 0x01 }, acknowledgeReceiveTimeoutMilliseconds: 1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            var sendCommandResult = await sendCommandTask;

            zvtCommunication.DataReceived -= dataReceived;
            zvtCommunication.Dispose();

            Assert.AreEqual(SendCommandResult.PositiveCompletionReceived, sendCommandResult);
            Assert.IsFalse(additionalDataReceived, "Here are no additional data");
        }

        [TestMethod]
        public async Task SendCommandAsync_AcknowledgeReceivedWithDataFragment_Successful()
        {
            var additionalDataReceived = false;
            byte[] additionalData = null;

            ProcessData dataReceived(byte[] data)
            {
                additionalDataReceived = true;
                additionalData = data;
                return new ProcessData{ State = ProcessDataState.Processed };
            }

            var loggerZvtCommunication = LoggerHelper.GetLogger<ZvtCommunication>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var zvtCommunication = new ZvtCommunication(loggerZvtCommunication.Object, mockDeviceCommunication.Object);
            zvtCommunication.DataReceived += dataReceived;

            var sendCommandTask = zvtCommunication.SendCommandAsync(new byte[] { 0x01 }, acknowledgeReceiveTimeoutMilliseconds: 1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00, 0x01, 0x02 });
            var sendCommandResult = await sendCommandTask;

            zvtCommunication.DataReceived -= dataReceived;
            zvtCommunication.Dispose();

            Assert.AreEqual(SendCommandResult.PositiveCompletionReceived, sendCommandResult);
            Assert.IsTrue(additionalDataReceived, "Additional data not forwarded");
            Assert.IsTrue(Enumerable.SequenceEqual(new byte[] { 0x01, 0x02 }, additionalData), "Wrong additional data");
        }

        [TestMethod]
        public async Task SendCommandAsync_AcknowledgeReceivedTooLate_Successful()
        {
            var timeout = 1000;

            var loggerZvtCommunication = LoggerHelper.GetLogger<ZvtCommunication>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var zvtCommunication = new ZvtCommunication(loggerZvtCommunication.Object, mockDeviceCommunication.Object);

            var sendCommandTask = zvtCommunication.SendCommandAsync(new byte[] { 0x01 }, acknowledgeReceiveTimeoutMilliseconds: timeout);
            await Task.Delay(timeout + 100);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            var sendCommandResult = await sendCommandTask;

            zvtCommunication.Dispose();

            Assert.AreEqual(SendCommandResult.NoDataReceived, sendCommandResult);
        }

        [TestMethod]
        public async Task SendCommandAsync_NegativeCompletionReceived_Successful()
        {
            var loggerZvtCommunication = LoggerHelper.GetLogger<ZvtCommunication>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var zvtCommunication = new ZvtCommunication(loggerZvtCommunication.Object, mockDeviceCommunication.Object);

            var sendCommandTask = zvtCommunication.SendCommandAsync(new byte[] { 0x01 }, acknowledgeReceiveTimeoutMilliseconds: 1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x84, 0x01, 0x00 });
            var sendCommandResult = await sendCommandTask;

            zvtCommunication.Dispose();

            Assert.AreEqual(SendCommandResult.NegativeCompletionReceived, sendCommandResult);
        }

        [TestMethod]
        public async Task SendCommandAsync_ReceiveInvalidData_Successful()
        {
            var loggerZvtCommunication = LoggerHelper.GetLogger<ZvtCommunication>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var zvtCommunication = new ZvtCommunication(loggerZvtCommunication.Object, mockDeviceCommunication.Object);

            var sendCommandTask = zvtCommunication.SendCommandAsync(new byte[] { 0x01 }, acknowledgeReceiveTimeoutMilliseconds: 1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x01, 0x02, 0x03 });
            var sendCommandResult = await sendCommandTask;

            zvtCommunication.Dispose();

            Assert.AreEqual(SendCommandResult.UnknownFailure, sendCommandResult);
        }

        [TestMethod]
        public async Task SendCommandAsync_NotSupported_Successful()
        {
            var loggerZvtCommunication = LoggerHelper.GetLogger<ZvtCommunication>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var zvtCommunication = new ZvtCommunication(loggerZvtCommunication.Object, mockDeviceCommunication.Object);

            var sendCommandTask = zvtCommunication.SendCommandAsync(new byte[] { 0x01 }, acknowledgeReceiveTimeoutMilliseconds: 1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x84, 0x83, 0x00 });
            var sendCommandResult = await sendCommandTask;

            zvtCommunication.Dispose();

            Assert.AreEqual(SendCommandResult.NotSupported, sendCommandResult);
        }

        [TestMethod]
        public async Task SendCommandAsync_NoDataReceived_Successful()
        {
            var loggerZvtCommunication = LoggerHelper.GetLogger<ZvtCommunication>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var zvtCommunication = new ZvtCommunication(loggerZvtCommunication.Object, mockDeviceCommunication.Object);

            var sendCommandResult = await zvtCommunication.SendCommandAsync(new byte[] { 0x01 }, acknowledgeReceiveTimeoutMilliseconds: 1000);

            zvtCommunication.Dispose();

            Assert.AreEqual(SendCommandResult.NoDataReceived, sendCommandResult);
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Portalum.Zvt.Models;
using System.Threading.Tasks;

namespace Portalum.Zvt.UnitTest
{
    [TestClass]
    public class ZvtCommunicationTest
    {
        [TestMethod]
        public async Task SendCommandAsync_AcknowledgeReceived_Successful()
        {
            var loggerZvtCommunication = LoggerHelper.GetLogger<ZvtCommunication>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var zvtCommunication = new ZvtCommunication(loggerZvtCommunication.Object, mockDeviceCommunication.Object);

            var sendCommandTask = zvtCommunication.SendCommandAsync(new byte[] { 0x01 }, acknowledgeReceiveTimeoutMilliseconds: 1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            var sendCommandResult = await sendCommandTask;

            zvtCommunication.Dispose();

            Assert.AreEqual(SendCommandResult.PositiveCompletionReceived, sendCommandResult);
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace Portalum.Zvt.UnitTest
{
    [TestClass]
    public class ZvtClientTest
    {
        [TestMethod]
        public async Task EndOfDayAsync_AcknowledgeReceivedWithoutCompletionReceived_Successful()
        {
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);

            var endOfDayTask = zvtClient.EndOfDayAsync();
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            var commandResponse = await endOfDayTask;

            zvtClient.Dispose();

            Assert.AreEqual(CommandResponseState.Timeout, commandResponse.State);
        }

        [TestMethod]
        public async Task EndOfDayAsync_AcknowledgeReceivedWithCompletionReceived_Successful()
        {
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);

            var endOfDayTask = zvtClient.EndOfDayAsync();
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            await Task.Delay(1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x0F, 0x00 });
            var commandResponse = await endOfDayTask;

            zvtClient.Dispose();

            Assert.AreEqual(CommandResponseState.Successful, commandResponse.State);
        }

        [TestMethod]
        public async Task EndOfDayAsync_AcknowledgeReceivedWithAbortReceived1_Successful()
        {
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);

            var endOfDayTask = zvtClient.EndOfDayAsync();
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            await Task.Delay(1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x1E, 0x00 });
            var commandResponse = await endOfDayTask;

            zvtClient.Dispose();

            Assert.AreEqual(CommandResponseState.Abort, commandResponse.State);
        }

        [TestMethod]
        public async Task EndOfDayAsync_AcknowledgeReceivedWithAbortReceived2_Successful()
        {
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);

            var endOfDayTask = zvtClient.EndOfDayAsync();
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            await Task.Delay(1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x1E, 0x01, 0x64 });
            var commandResponse = await endOfDayTask;

            zvtClient.Dispose();

            Assert.AreEqual(CommandResponseState.Abort, commandResponse.State);
            Assert.AreEqual("card not readable (LRC-/parity-error)", commandResponse.ErrorMessage);
        }
    }
}

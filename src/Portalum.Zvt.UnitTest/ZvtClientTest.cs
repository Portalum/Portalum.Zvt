using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Portalum.Zvt.Helpers;

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

        [TestMethod]
        public async Task PaymentAsync_SuccessfulPayment_Successful()
        {
            byte[] dataSent = Array.Empty<byte>();
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();
            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task(byte[] data, CancellationToken token) =>
                {
                    dataSent = data;
                    return Task.CompletedTask;
                });

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);

            var paymentTask = zvtClient.PaymentAsync(10);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            // ensure the timeout is not set, when nothing is passed to PaymentAsync
            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x07, 0x04, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 },
                dataSent);

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null,
                new byte[] { 0x04, 0x0F, 0x02, 0x27, 0x00 });
            await Task.Delay(1000);

            // check that the ECR answers immediately, as no issueGoodsCallback is set
            CollectionAssert.AreEqual(new byte[] { 0x80, 0x00, 0x00 }, dataSent);

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x0F, 0x00 });
            await Task.Delay(1000);
            var commandResponse = await paymentTask;

            zvtClient.Dispose();
            Assert.AreEqual(CommandResponseState.Successful, commandResponse.State);
        }

        [TestMethod]
        public async Task PaymentAsync_CardRejected_Successful()
        {
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);

            var paymentTask = zvtClient.PaymentAsync(10);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });

            var dataHex = "04-0F-25-27-05-29-29-00-02-74-3C-F0-F0-F9-41-62-67-65-" +
                          "6C-65-68-6E-74-8A-06-06-0D-24-0B-07-09-41-62-67-65-6C-65-68-6E-74";
            var cardRejectedStatusInformation = ByteHelper.HexToByteArray(dataHex);

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, cardRejectedStatusInformation);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x1E, 0x01, 0x05 });
            await Task.Delay(1000);
            var commandResponse = await paymentTask;

            zvtClient.Dispose();
            Assert.AreEqual(CommandResponseState.Abort, commandResponse.State);
        }

        [TestMethod]
        public async Task PaymentAsync_IssueOfGoods_DelayedSuccess_Successful()
        {
            var taskStarted = false;
            byte[] dataSent = Array.Empty<byte>();
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();
            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task(byte[] data, CancellationToken token) =>
                {
                    dataSent = data;
                    return Task.CompletedTask;
                });

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);

            var issueGoodsAfter3Seconds = async Task<bool>() =>
            {
                taskStarted = true;
                await Task.Delay(3000);
                return true;
            };
            
            var paymentTask =
                zvtClient.PaymentAsync(33, issueGoodsCallback: issueGoodsAfter3Seconds, issueGoodsTimeout: 5);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x09, 0x04, 0x00, 0x00, 0x00, 0x00, 0x33, 0x00, 0x01, 0x05 },
                dataSent);
            
            dataSent = Array.Empty<byte>();
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null,
                new byte[] { 0x04, 0x0F, 0x02, 0x27, 0x00 });
            await Task.Delay(1000);

            // after the status information has been received, the fulfill task is started 
            Assert.IsTrue(taskStarted);

            // ensure no answer is sent, until the task has finished
            CollectionAssert.AreEqual(Array.Empty<byte>(), dataSent);

            // after the task has finished, the answer is sent
            await Task.Delay(3000); // 2 seconds remaining, 1 second for the ECR to answer
            CollectionAssert.AreEqual(new byte[] { 0x80, 0x00, 0x00 }, dataSent);
            
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x0F, 0x00 });
            var commandResponse = await paymentTask;

            zvtClient.Dispose();
            Assert.AreEqual(CommandResponseState.Successful, commandResponse.State);
        }
        
        [TestMethod]
        public async Task PaymentAsync_IssueOfGoods_DelayedFailure_Successful()
        {
            var taskStarted = false;
            byte[] dataSent = Array.Empty<byte>();
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();
            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task(byte[] data, CancellationToken token) =>
                {
                    dataSent = data;
                    return Task.CompletedTask;
                });

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);

            var issueGoodsAfter3Seconds = async Task<bool>() =>
            {
                taskStarted = true;
                await Task.Delay(3000);
                return false;
            };
            
            var paymentTask =
                zvtClient.PaymentAsync(33, issueGoodsCallback: issueGoodsAfter3Seconds, issueGoodsTimeout: 5);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x09, 0x04, 0x00, 0x00, 0x00, 0x00, 0x33, 0x00, 0x01, 0x05 },
                dataSent);
            
            dataSent = Array.Empty<byte>();
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null,
                new byte[] { 0x04, 0x0F, 0x02, 0x27, 0x00 });
            await Task.Delay(1000);

            // after the status information has been received, the fulfill task is started 
            Assert.IsTrue(taskStarted);

            // ensure no answer is sent, until the task has finished
            CollectionAssert.AreEqual(Array.Empty<byte>(), dataSent);

            // after the task has finished, the answer is sent
            await Task.Delay(3000); // 2 seconds remaining, 1 second for the ECR to answer
            CollectionAssert.AreEqual(new byte[] { 0x84, 0x66, 0x00 }, dataSent);
            
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x1E, 0x01, 0x05 });
            var commandResponse = await paymentTask;

            zvtClient.Dispose();
            Assert.AreEqual(CommandResponseState.Abort, commandResponse.State);
        }
        
        [TestMethod]
        public async Task PaymentAsync_IssueOfGoods_RejectedCard_Successful()
        {
            var taskStarted = false;
            byte[] dataSent = Array.Empty<byte>();
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();
            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task(byte[] data, CancellationToken token) =>
                {
                    dataSent = data;
                    return Task.CompletedTask;
                });

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);

            var issueGoodsAfter3Seconds = async Task<bool>() =>
            {
                taskStarted = true;
                await Task.Delay(3000);
                return true;
            };
            
            var paymentTask =
                zvtClient.PaymentAsync(33, issueGoodsCallback: issueGoodsAfter3Seconds, issueGoodsTimeout: 5);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x09, 0x04, 0x00, 0x00, 0x00, 0x00, 0x33, 0x00, 0x01, 0x05 },
                dataSent);
            
            dataSent = Array.Empty<byte>();
            var dataHex = "04-0F-25-27-05-29-29-00-02-74-3C-F0-F0-F9-41-62-67-65-" +
                          "6C-65-68-6E-74-8A-06-06-0D-24-0B-07-09-41-62-67-65-6C-65-68-6E-74";
            var cardRejectedStatusInformation = ByteHelper.HexToByteArray(dataHex);

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, cardRejectedStatusInformation);
            await Task.Delay(1000);

            // a not successful status information does not result in the issue goods task beeing started 
            Assert.IsFalse(taskStarted);

            // ensure we answer with an ack in this case
            CollectionAssert.AreEqual(new byte[] { 0x80, 0x00, 0x00 }, dataSent);
            
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x1E, 0x01, 0x05 });
            var commandResponse = await paymentTask;

            zvtClient.Dispose();
            Assert.AreEqual(CommandResponseState.Abort, commandResponse.State);
        }
    }
}
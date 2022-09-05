using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Portalum.Zvt.Helpers;
using Portalum.Zvt.Models;

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
            var dataSent = Array.Empty<byte>();
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();
            var dataSentCancellationTokenSource = new CancellationTokenSource();
            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task(byte[] data, CancellationToken token) =>
                {
                    dataSent = data;
                    dataSentCancellationTokenSource.Cancel();
                    return Task.CompletedTask;
                });

            var startAsyncCompletionCalled = false;
            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);
            zvtClient.StartAsyncCompletion += (_) => startAsyncCompletionCalled = true;
            var paymentTask = zvtClient.PaymentAsync(10);
            await Task.Delay(1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            // ensure the timeout is not set, when nothing is passed to PaymentAsync
            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x07, 0x04, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 },
                dataSent);

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null,
                new byte[] { 0x04, 0x0F, 0x02, 0x27, 0x00 });
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            // check that the ECR answers immediately, as no issueGoodsCallback is set
            CollectionAssert.AreEqual(new byte[] { 0x80, 0x00, 0x00 }, dataSent);

            dataSentCancellationTokenSource = new CancellationTokenSource();
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x0F, 0x00 });
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });
            var commandResponse = await paymentTask;
            Assert.IsFalse(startAsyncCompletionCalled);

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
            await Task.Delay(1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });

            var dataHex = "04-0F-25-27-05-29-29-00-02-74-3C-F0-F0-F9-41-62-67-65-6C-65-68-6E-74-8A-06-06-0D-24-0B-07-09-41-62-67-65-6C-65-68-6E-74";
            var cardRejectedStatusInformation = ByteHelper.HexToByteArray(dataHex);

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, cardRejectedStatusInformation);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x1E, 0x01, 0x05 });
            var commandResponse = await paymentTask;

            zvtClient.Dispose();
            Assert.AreEqual(CommandResponseState.Abort, commandResponse.State);
        }

        [TestMethod]
        public async Task PaymentAsync_IssueOfGoods_DelayedSuccess_Successful()
        {
            var dataSent = Array.Empty<byte>();
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();
            var dataSentCancellationTokenSource = new CancellationTokenSource();
            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task(byte[] data, CancellationToken token) =>
                {
                    dataSent = data;
                    dataSentCancellationTokenSource.Cancel();
                    return Task.CompletedTask;
                });

            var startAsyncCompletionLaunchCount = 0;
            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);
            var completionInfo = new CompletionInfo();

            zvtClient.StartAsyncCompletion += (_) => startAsyncCompletionLaunchCount++;
            zvtClient.GetAsyncCompletionInfo += () => completionInfo;

            var paymentTask = zvtClient.PaymentAsync(33);
            await Task.Delay(1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x09, 0x04, 0x00, 0x00, 0x00, 0x00, 0x33, 0x00, 0x02, 0x0A }, dataSent);

            dataSent = Array.Empty<byte>();
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x04, 0x0F, 0x02, 0x27, 0x00 });
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            // the ECR immediately requests for a timeout-extension
            CollectionAssert.AreEqual(new byte[] { 0x84, 0x9C, 0x00 }, dataSent);

            // if the completion info indicates a success with a changed amount ...
            completionInfo.State = CompletionInfoState.ChangeAmount;
            completionInfo.Amount = 22m;
            dataSentCancellationTokenSource = new CancellationTokenSource();
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x04, 0x0F, 0x02, 0x27, 0x00 });
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });
            
            // ... the PT is informed about the changed amount
            CollectionAssert.AreEqual(new byte[] { 0x84, 0x9D, 0x07, 0x04, 0x00, 0x00, 0x00, 0x00, 0x22, 0x00 }, dataSent);

            // the start completion event MUST only be triggered once, even tough we have received two status information events
            Assert.AreEqual(1, startAsyncCompletionLaunchCount);
            
            // the pt will send a positive completion
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x0F, 0x00 });
            var commandResponse = await paymentTask;

            zvtClient.Dispose();
            Assert.AreEqual(CommandResponseState.Successful, commandResponse.State);
        }

        [TestMethod]
        public async Task PaymentAsync_IssueOfGoods_DelayedFailure_Successful()
        {
            var dataSent = Array.Empty<byte>();
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();
            var dataSentCancellationTokenSource = new CancellationTokenSource();
            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task(byte[] data, CancellationToken token) =>
                {
                    dataSent = data;
                    dataSentCancellationTokenSource.Cancel();
                    return Task.CompletedTask;
                });

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);
            var completionInfo = new CompletionInfo();

            zvtClient.GetAsyncCompletionInfo += () => completionInfo;

            var paymentTask = zvtClient.PaymentAsync(33);
            await Task.Delay(1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x09, 0x04, 0x00, 0x00, 0x00, 0x00, 0x33, 0x00, 0x02, 0x0A }, dataSent);

            dataSent = Array.Empty<byte>();
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x04, 0x0F, 0x02, 0x27, 0x00 });
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            // the ECR immediately requests for a timeout-extension
            CollectionAssert.AreEqual(new byte[] { 0x84, 0x9C, 0x00 }, dataSent);

            // if the completion info indicates a failure, the status information's response changes
            completionInfo.State = CompletionInfoState.Failure;
            dataSentCancellationTokenSource = new CancellationTokenSource();
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x04, 0x0F, 0x02, 0x27, 0x00 });
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });
            CollectionAssert.AreEqual(new byte[] { 0x84, 0x66, 0x00 }, dataSent);

            // the pt will send a negative completion
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x1E, 0x01, 0x05 });
            var commandResponse = await paymentTask;

            zvtClient.Dispose();
            Assert.AreEqual(CommandResponseState.Abort, commandResponse.State);
        }

        [TestMethod]
        public async Task PaymentAsync_IssueOfGoods_RejectedCard_Successful()
        {
            var dataSent = Array.Empty<byte>();
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();
            var dataSentCancellationTokenSource = new CancellationTokenSource();
            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task(byte[] data, CancellationToken token) =>
                {
                    dataSent = data;
                    dataSentCancellationTokenSource.Cancel();
                    return Task.CompletedTask;
                });

            var startAsyncCompletionCalled = false;
            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);
            var completionInfo = new CompletionInfo();
            var askForCompletionCalled = false;
            
            zvtClient.StartAsyncCompletion += (_) => startAsyncCompletionCalled = true;
            zvtClient.GetAsyncCompletionInfo += () =>
            {
                askForCompletionCalled = true;
                return completionInfo;
            };

            var paymentTask = zvtClient.PaymentAsync(33);
            await Task.Delay(1000);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x80, 0x00, 0x00 });
            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x09, 0x04, 0x00, 0x00, 0x00, 0x00, 0x33, 0x00, 0x02, 0x0A }, dataSent);

            dataSent = Array.Empty<byte>();
            var negativeAuthorization = "04-0F-25-27-05-29-29-00-02-74-3C-F0-F0-F9-41-62-67-65-6C-65-68-6E-74-8A-06-06-0D-24-0B-07-09-41-62-67-65-6C-65-68-6E-74";
            var cardRejectedStatusInformation = ByteHelper.HexToByteArray(negativeAuthorization);

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, cardRejectedStatusInformation);
            await Task.Delay(5000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });
            
            // a not successful status must neither trigger the askForCompletion nor the startCompletion events
            Assert.IsFalse(askForCompletionCalled);
            Assert.IsFalse(startAsyncCompletionCalled);

            // ensure we answer with an ack in this case
            CollectionAssert.AreEqual(new byte[] { 0x80, 0x00, 0x00 }, dataSent);

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x1E, 0x01, 0x05 });
            var commandResponse = await paymentTask;

            zvtClient.Dispose();
            Assert.AreEqual(CommandResponseState.Abort, commandResponse.State);
        }
    }
}

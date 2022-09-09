using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Portalum.Zvt.Helpers;
using Portalum.Zvt.Models;
using System;
using System.Threading;
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

        [TestMethod]
        public async Task PaymentAsync_SuccessfulPayment_Successful()
        {
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            StatusInformation receivedStatusInformation = null;

            void statusInformationReceived(StatusInformation statusInformation)
            {
                receivedStatusInformation = statusInformation;
            }

            var dataSent = Array.Empty<byte>();
            var dataSentCancellationTokenSource = new CancellationTokenSource();

            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] data, CancellationToken cancellationToken) =>
                {
                    dataSent = data;
                    dataSentCancellationTokenSource?.Cancel();
                    return true;
                });

            var startAsyncCompletionCalled = false;
            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);
            zvtClient.StatusInformationReceived += statusInformationReceived;
            zvtClient.CompletionStartReceived += (_) => startAsyncCompletionCalled = true;

            var paymentTask = zvtClient.PaymentAsync(10);
            await Task.Delay(500, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            dataSentCancellationTokenSource.Dispose();
            dataSentCancellationTokenSource = new CancellationTokenSource();

            // ensure the timeout is not set, when nothing is passed to PaymentAsync
            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x07, 0x04, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00 }, dataSent, $"Invalid Payment command");

            dataSent = Array.Empty<byte>();

            var paymentTerminalPositiveCompletion = new byte[] { 0x80, 0x00, 0x00 };
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalPositiveCompletion);

            var paymentTerminalStatusInformation = new byte[] { 0x04, 0x0F, 0x02, 0x27, 0x00 };
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalStatusInformation);
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            Assert.IsNotNull(receivedStatusInformation);
            receivedStatusInformation = null;

            // check that the ECR answers immediately, as no issueGoodsCallback is set
            var electronicCashRegisterAcknowlegeForStatusInformation = new byte[] { 0x80, 0x00, 0x00 };
            CollectionAssert.AreEqual(electronicCashRegisterAcknowlegeForStatusInformation, dataSent, $"Collection is wrong {BitConverter.ToString(dataSent)}");

            dataSentCancellationTokenSource.Dispose();
            dataSentCancellationTokenSource = new CancellationTokenSource();

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x0F, 0x00 });
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            var commandResponse = await paymentTask;
            Assert.IsFalse(startAsyncCompletionCalled);

            zvtClient.StatusInformationReceived -= statusInformationReceived;

            zvtClient.Dispose();
            dataSentCancellationTokenSource.Dispose();

            Assert.AreEqual(CommandResponseState.Successful, commandResponse.State);
        }

        [TestMethod]
        public async Task PaymentAsync_CardRejected_Successful()
        {
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            StatusInformation receivedStatusInformation = null;

            void statusInformationReceived(StatusInformation statusInformation)
            {
                receivedStatusInformation = statusInformation;
            }

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);
            zvtClient.StatusInformationReceived += statusInformationReceived;

            //Start the payment process async
            var paymentTask = zvtClient.PaymentAsync(10);
            //short wait to send the command to the payment terminal
            await Task.Delay(500);

            var paymentTerminalPositiveCompletion = new byte[] { 0x80, 0x00, 0x00 };
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalPositiveCompletion);

            await Task.Delay(50);

            //Status Information package from the payment terminal
            var dataHex = "04-0F-25-27-05-29-29-00-02-74-3C-F0-F0-F9-41-62-67-65-6C-65-68-6E-74-8A-06-06-0D-24-0B-07-09-41-62-67-65-6C-65-68-6E-74";
            var paymentTerminalCardRejectedStatusInformation = ByteHelper.HexToByteArray(dataHex);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalCardRejectedStatusInformation);

            await Task.Delay(50);

            var paymentTerminalAbortInfo = new byte[] { 0x06, 0x1E, 0x01, 0x05 };
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalAbortInfo);

            var commandResponse = await paymentTask;

            zvtClient.StatusInformationReceived -= statusInformationReceived;
            zvtClient.Dispose();

            Assert.AreEqual(CommandResponseState.Abort, commandResponse.State);
            Assert.AreEqual(29000274, receivedStatusInformation.TerminalIdentifier);
            Assert.AreEqual("Abgelehnt", receivedStatusInformation.AdditionalText);
            Assert.AreEqual("Mastercard", receivedStatusInformation.CardType);
        }

        [TestMethod]
        public async Task PaymentAsync_IssueOfGoods_DelayedSuccess_Successful()
        {
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            StatusInformation receivedStatusInformation = null;

            void statusInformationReceived(StatusInformation statusInformation)
            {
                receivedStatusInformation = statusInformation;
            }

            var dataSent = Array.Empty<byte>();
            var dataSentCancellationTokenSource = new CancellationTokenSource();

            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] data, CancellationToken cancellationToken) =>
                {
                    dataSent = data;
                    dataSentCancellationTokenSource?.Cancel();
                    return true;
                });

            var startAsyncCompletionLaunchCount = 0;
            var completionInfo = new CompletionInfo
            {
                State = CompletionInfoState.Wait
            };

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);
            zvtClient.StatusInformationReceived += statusInformationReceived;

            zvtClient.CompletionStartReceived += (_) => startAsyncCompletionLaunchCount++;
            zvtClient.CompletionDecisionRequested += () => completionInfo;

            var paymentTask = zvtClient.PaymentAsync(33);

            await Task.Delay(500, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });
            //Recreate Cancellation Token
            dataSentCancellationTokenSource.Dispose();
            dataSentCancellationTokenSource = new CancellationTokenSource();

            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x09, 0x04, 0x00, 0x00, 0x00, 0x00, 0x33, 0x00, 0x02, 0x0A }, dataSent, $"Invalid Payment command");
            dataSent = Array.Empty<byte>();

            var paymentTerminalPositiveCompletion = new byte[] { 0x80, 0x00, 0x00 };
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalPositiveCompletion);

            await Task.Delay(50);

            var paymentTerminalStatusInformation = new byte[] { 0x04, 0x0F, 0x02, 0x27, 0x00 };
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalStatusInformation);

            Assert.IsNotNull(receivedStatusInformation);
            receivedStatusInformation = null;
            
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            // the ECR immediately requests for a timeout-extension
            CollectionAssert.AreEqual(new byte[] { 0x84, 0x9C, 0x00 }, dataSent, $"Collection is wrong {BitConverter.ToString(dataSent)}");
            dataSent = Array.Empty<byte>();

            // if the completion info indicates a success with a changed amount ...
            completionInfo.State = CompletionInfoState.ChangeAmount;
            completionInfo.Amount = 22m;

            //Recreate Cancellation Token
            dataSentCancellationTokenSource.Dispose();
            dataSentCancellationTokenSource = new CancellationTokenSource();

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalStatusInformation);
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            // ... the PT is informed about the changed amount
            CollectionAssert.AreEqual(new byte[] { 0x84, 0x9D, 0x07, 0x04, 0x00, 0x00, 0x00, 0x00, 0x22, 0x00 }, dataSent, $"Collection is wrong {BitConverter.ToString(dataSent)}");
            dataSent = Array.Empty<byte>();

            // the start completion event MUST only be triggered once, even tough we have received two status information events
            Assert.AreEqual(1, startAsyncCompletionLaunchCount);

            //// the pt will send a positive completion
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x0F, 0x00 });
            var commandResponse = await paymentTask;

            zvtClient.StatusInformationReceived -= statusInformationReceived;

            zvtClient.Dispose();
            dataSentCancellationTokenSource.Dispose();

            Assert.AreEqual(CommandResponseState.Successful, commandResponse.State);
        }

        [TestMethod]
        public async Task PaymentAsync_IssueOfGoods_DelayedFailure_Successful()
        {
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            StatusInformation receivedStatusInformation = null;

            void statusInformationReceived(StatusInformation statusInformation)
            {
                receivedStatusInformation = statusInformation;
            }

            var dataSent = Array.Empty<byte>();
            var dataSentCancellationTokenSource = new CancellationTokenSource();

            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] data, CancellationToken cancellationToken) =>
                {
                    dataSent = data;
                    dataSentCancellationTokenSource?.Cancel();
                    return true;
                });

            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var completionInfo = new CompletionInfo();

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);
            zvtClient.StatusInformationReceived += statusInformationReceived;
            zvtClient.CompletionDecisionRequested += () => completionInfo;

            var paymentTask = zvtClient.PaymentAsync(33);
            await Task.Delay(500, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x09, 0x04, 0x00, 0x00, 0x00, 0x00, 0x33, 0x00, 0x02, 0x0A }, dataSent, $"Collection is wrong {BitConverter.ToString(dataSent)}");
            dataSent = Array.Empty<byte>();

            var paymentTerminalPositiveCompletion = new byte[] { 0x80, 0x00, 0x00 };
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalPositiveCompletion);

            //Recreate Cancellation Token
            dataSentCancellationTokenSource.Dispose();
            dataSentCancellationTokenSource = new CancellationTokenSource();

            var paymentTerminalStatusInformation = new byte[] { 0x04, 0x0F, 0x02, 0x27, 0x00 };
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalStatusInformation);
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            Assert.IsNotNull(receivedStatusInformation);
            receivedStatusInformation = null;

            // the ECR immediately requests for a timeout-extension
            CollectionAssert.AreEqual(new byte[] { 0x84, 0x9C, 0x00 }, dataSent, $"Collection is wrong {BitConverter.ToString(dataSent)}");
            dataSent = Array.Empty<byte>();

            //Recreate Cancellation Token
            dataSentCancellationTokenSource.Dispose();
            dataSentCancellationTokenSource = new CancellationTokenSource();

            // if the completion info indicates a failure, the status information's response changes
            completionInfo.State = CompletionInfoState.Failure;

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalStatusInformation);
            await Task.Delay(3000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            Assert.IsNotNull(receivedStatusInformation);
            receivedStatusInformation = null;

            CollectionAssert.AreEqual(new byte[] { 0x84, 0x66, 0x00 }, dataSent, $"Collection is wrong {BitConverter.ToString(dataSent)}");
            dataSent = Array.Empty<byte>();

            // the pt will send a negative completion
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x1E, 0x01, 0x05 });
            var commandResponse = await paymentTask;

            zvtClient.StatusInformationReceived -= statusInformationReceived;

            zvtClient.Dispose();
            dataSentCancellationTokenSource.Dispose();

            Assert.AreEqual(CommandResponseState.Abort, commandResponse.State);
        }

        [TestMethod]
        public async Task PaymentAsync_IssueOfGoods_RejectedCard_Successful()
        {
            var loggerZvtClient = LoggerHelper.GetLogger<ZvtClient>();
            var mockDeviceCommunication = new Mock<IDeviceCommunication>();

            var dataSent = Array.Empty<byte>();
            var dataSentCancellationTokenSource = new CancellationTokenSource();

            mockDeviceCommunication
                .Setup(c => c.SendAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] data, CancellationToken cancellationToken) =>
                {
                    dataSent = data;
                    dataSentCancellationTokenSource?.Cancel();
                    return true;
                });

            var startAsyncCompletionCalled = false;
            var completionInfo = new CompletionInfo();
            var askForCompletionCalled = false;
            var clientConfig = new ZvtClientConfig
            {
                CommandCompletionTimeout = TimeSpan.FromSeconds(5)
            };

            var zvtClient = new ZvtClient(mockDeviceCommunication.Object, loggerZvtClient.Object, clientConfig);

            zvtClient.CompletionStartReceived += (_) => startAsyncCompletionCalled = true;
            zvtClient.CompletionDecisionRequested += () =>
            {
                askForCompletionCalled = true;
                return completionInfo;
            };

            var paymentTask = zvtClient.PaymentAsync(33);
            await Task.Delay(500, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            CollectionAssert.AreEqual(new byte[] { 0x06, 0x01, 0x09, 0x04, 0x00, 0x00, 0x00, 0x00, 0x33, 0x00, 0x02, 0x0A }, dataSent, $"Collection is wrong {BitConverter.ToString(dataSent)}");
            dataSent = Array.Empty<byte>();

            var paymentTerminalPositiveCompletion = new byte[] { 0x80, 0x00, 0x00 };
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, paymentTerminalPositiveCompletion);

            //Recreate Cancellation Token
            dataSentCancellationTokenSource.Dispose();
            dataSentCancellationTokenSource = new CancellationTokenSource();

            var negativeAuthorization = "04-0F-25-27-05-29-29-00-02-74-3C-F0-F0-F9-41-62-67-65-6C-65-68-6E-74-8A-06-06-0D-24-0B-07-09-41-62-67-65-6C-65-68-6E-74";
            var cardRejectedStatusInformation = ByteHelper.HexToByteArray(negativeAuthorization);
            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, cardRejectedStatusInformation);

            await Task.Delay(5000, dataSentCancellationTokenSource.Token).ContinueWith(_ => { });

            // a not successful status must neither trigger the askForCompletion nor the startCompletion events
            Assert.IsFalse(askForCompletionCalled);
            Assert.IsFalse(startAsyncCompletionCalled);

            // ensure we answer with an ack in this case
            CollectionAssert.AreEqual(new byte[] { 0x80, 0x00, 0x00 }, dataSent, $"Collection is wrong {BitConverter.ToString(dataSent)}");
            dataSent = Array.Empty<byte>();

            mockDeviceCommunication.Raise(mock => mock.DataReceived += null, new byte[] { 0x06, 0x1E, 0x01, 0x05 });
            var commandResponse = await paymentTask;

            zvtClient.Dispose();
            dataSentCancellationTokenSource.Dispose();

            Assert.AreEqual(CommandResponseState.Abort, commandResponse.State);
        }
    }
}

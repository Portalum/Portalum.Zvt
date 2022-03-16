using Microsoft.Extensions.Logging;
using Portalum.Zvt.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Portalum.Zvt
{
    /// <summary>
    /// ZvtCommunication, automatic completion processing
    /// This middle layer filters out completion packages and forwards the other data
    /// </summary>
    public class ZvtCommunication : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IDeviceCommunication _deviceCommunication;

        private CancellationTokenSource _acknowledgeReceivedCancellationTokenSource;
        private byte[] _dataBuffer;
        private bool _waitForAcknowledge = false;

        /// <summary>
        /// New data received from the pt device
        /// </summary>
        public event Action<byte[]> DataReceived;

        private readonly byte[] _positiveCompletionData1 = new byte[] { 0x80, 0x00, 0x00 }; //Default
        private readonly byte[] _positiveCompletionData2 = new byte[] { 0x84, 0x00, 0x00 }; //Alternative
        private readonly byte[] _positiveCompletionData3 = new byte[] { 0x84, 0x9C, 0x00 }; //Special case for request more time
        private readonly byte _negativeCompletionPrefix = 0x84;

        /// <summary>
        /// ZvtCommunication
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="deviceCommunication"></param>
        public ZvtCommunication(
            ILogger logger,
            IDeviceCommunication deviceCommunication)
        {
            this._logger = logger;
            this._deviceCommunication = deviceCommunication;
            this._deviceCommunication.DataReceived += this.ProcessDataReceived;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._deviceCommunication.DataReceived -= this.ProcessDataReceived;
            }
        }

        /// <summary>
        /// Switch for incoming data
        /// </summary>
        /// <param name="data"></param>
        private void ProcessDataReceived(byte[] data)
        {
            if (this._waitForAcknowledge)
            {
                this._dataBuffer = data;
                this._acknowledgeReceivedCancellationTokenSource?.Cancel();
                return;
            }

            //TODO: Send only one completion for fragmented data
            //Send acknowledge before process the data
            this._deviceCommunication.SendAsync(this._positiveCompletionData1);

            //TODO: Connect receive handler with a response
            this.DataReceived?.Invoke(data);
        }

        /// <summary>
        /// Send command
        /// </summary>
        /// <param name="commandData">The data of the command</param>
        /// <param name="acknowledgeReceiveTimeoutMilliseconds">Maximum waiting time for the acknowledge package, default is 5 seconds, T3 Timeout</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SendCommandResult> SendCommandAsync(
            byte[] commandData,
            int acknowledgeReceiveTimeoutMilliseconds = 5000,
            CancellationToken cancellationToken = default)
        {
            this._acknowledgeReceivedCancellationTokenSource?.Dispose();
            this._acknowledgeReceivedCancellationTokenSource = new CancellationTokenSource();

            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this._acknowledgeReceivedCancellationTokenSource.Token);

            this._waitForAcknowledge = true;
            try
            {
                await this._deviceCommunication.SendAsync(commandData, linkedCancellationTokenSource.Token).ContinueWith(task => { });
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, $"{nameof(SendCommandAsync)} - Cannot send data");
                this._acknowledgeReceivedCancellationTokenSource.Dispose();
                return SendCommandResult.SendFailure;
            }

            await Task.Delay(acknowledgeReceiveTimeoutMilliseconds, linkedCancellationTokenSource.Token).ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    this._logger.LogError($"{nameof(SendCommandAsync)} - Wait task for acknowledge was aborted");
                }

                this._waitForAcknowledge = false;
            });

            this._acknowledgeReceivedCancellationTokenSource.Dispose();

            if (this._dataBuffer == null)
            {
                return SendCommandResult.NoDataReceived;
            }

            if (this.CheckIsPositiveCompletion())
            {
                return SendCommandResult.PositiveCompletionReceived;
            }

            if (this.CheckIsNegativeCompletion())
            {
                this._logger.LogError($"{nameof(SendCommandAsync)} - 'Negative completion' received");
                return SendCommandResult.NegativeCompletionReceived;
            }

            return SendCommandResult.UnknownFailure;
        }

        private bool CheckIsPositiveCompletion()
        {
            if (this._dataBuffer.Length < 3)
            {
                return false;
            }

            var buffer = this._dataBuffer.AsSpan().Slice(0, 3);

            if (buffer.SequenceEqual(this._positiveCompletionData1))
            {
                return true;
            }

            if (buffer.SequenceEqual(this._positiveCompletionData2))
            {
                return true;
            }

            if (buffer.SequenceEqual(this._positiveCompletionData3))
            {
                return true;
            }

            return false;
        }

        private bool CheckIsNegativeCompletion()
        {
            if (this._dataBuffer.Length < 3)
            {
                return false;
            }

            if (this._dataBuffer[0] == this._negativeCompletionPrefix)
            {
                var errorByte = this._dataBuffer[1];
                this._logger.LogDebug($"{nameof(CheckIsNegativeCompletion)} - ErrorCode:{errorByte:X2}");

                return true;
            }

            return false;
        }

        private void ForwardUnusedBufferData()
        {
            if (this._dataBuffer.Length == 3)
            {
                this._dataBuffer = null;
                return;
            }

            this._dataBuffer.AsSpan().Slice(3).ToArray();
            //TODO: Forward the unused data to ReceiveHandler
        }
    }
}

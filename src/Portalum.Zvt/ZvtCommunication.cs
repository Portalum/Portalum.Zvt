using Microsoft.Extensions.Logging;
using Portalum.Zvt.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Portalum.Zvt
{
    /// <summary>
    /// ZvtCommunication, automatic acknowledge processing
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

        private readonly byte[] _acknowledge = new byte[] { 0x80, 0x00, 0x00 };

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

        private void ProcessDataReceived(byte[] data)
        {
            if (this._waitForAcknowledge)
            {
                this._dataBuffer = data;
                this._acknowledgeReceivedCancellationTokenSource?.Cancel();
                return;
            }

            //Send acknowledge before process the data
            this._deviceCommunication.SendAsync(this._acknowledge);

            this.DataReceived?.Invoke(data);
        }

        /// <summary>
        /// Send command
        /// </summary>
        /// <param name="data"></param>
        /// <param name="acknowledgeReceiveTimeout">T3 Timeout in milliseconds, default 5 seconds</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<SendCommandResult> SendCommandAsync(
            byte[] data,
            int acknowledgeReceiveTimeout = 5000,
            CancellationToken cancellationToken = default)
        {
            this._acknowledgeReceivedCancellationTokenSource?.Dispose();
            this._acknowledgeReceivedCancellationTokenSource = new CancellationTokenSource();

            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this._acknowledgeReceivedCancellationTokenSource.Token);

            this._waitForAcknowledge = true;
            try
            {
                await this._deviceCommunication.SendAsync(data);
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, $"{nameof(SendCommandAsync)} - Cannot send data");
                this._acknowledgeReceivedCancellationTokenSource.Dispose();
                return SendCommandResult.SendFailure;
            }

            await Task.Delay(acknowledgeReceiveTimeout, linkedCancellationTokenSource.Token).ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    this._logger.LogError($"{nameof(SendCommandAsync)} - Wait for acknowledge is aborted");
                }

                this._waitForAcknowledge = false;
            });

            this._acknowledgeReceivedCancellationTokenSource.Dispose();

            if (this._dataBuffer == null)
            {
                return SendCommandResult.NoDataReceived;
            }

            if (this._dataBuffer.SequenceEqual(this._acknowledge))
            {
                return SendCommandResult.AcknowledgeReceived;
            }

            if (this._dataBuffer.Length > 2 && this._dataBuffer[0] == 0x84 && this._dataBuffer[1] != 0x00)
            {
                this._logger.LogError($"{nameof(SendCommandAsync)} - 'Negative completion' received");
                return SendCommandResult.NegativeCompletionReceived;
            }

            return SendCommandResult.UnknownFailure;
        }
    }
}

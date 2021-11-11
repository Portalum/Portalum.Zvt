using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Portalum.Payment.Zvt
{
    /// <summary>
    /// ZvtCommunication, automatic acknowledge processing
    /// </summary>
    public class ZvtCommunication : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IDeviceCommunication _deviceCommunication;
        private CancellationTokenSource _cancellationTokenSource;
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

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

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
                this._cancellationTokenSource?.Cancel();
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
        /// <param name="acknowledgeReceiveTimeout"></param>
        /// <returns></returns>
        public async Task<bool> SendCommandAsync(byte[] data, int acknowledgeReceiveTimeout = 5000)
        {
            this._cancellationTokenSource?.Dispose();
            this._cancellationTokenSource = new CancellationTokenSource();

            this._waitForAcknowledge = true;
            try
            {
                await this._deviceCommunication.SendAsync(data);
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, $"{nameof(SendCommandAsync)} - Cannot send data");
                this._cancellationTokenSource.Dispose();
                return false;
            }

            await Task.Delay(acknowledgeReceiveTimeout, this._cancellationTokenSource.Token).ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    this._logger.LogError($"{nameof(SendCommandAsync)} - No acknowlege received in the specified timeout {acknowledgeReceiveTimeout}ms");
                }

                this._waitForAcknowledge = false;
            });

            if (this._dataBuffer == null)
            {
                return false;
            }

            if (this._dataBuffer.SequenceEqual(this._acknowledge))
            {
                return true;
            }

            if (this._dataBuffer.Length > 2 && this._dataBuffer[0] == 0x84 && this._dataBuffer[1] != 0x00)
            {
                this._logger.LogError($"{nameof(SendCommandAsync)} - 'Negative completion' received");
                return false;
            }

            return false;
        }
    }
}

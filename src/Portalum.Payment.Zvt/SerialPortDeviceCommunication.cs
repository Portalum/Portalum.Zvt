using Microsoft.Extensions.Logging;
using Portalum.Payment.Zvt.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Portalum.Payment.Zvt
{
    /// <summary>
    /// SerialPort DeviceCommunication
    /// </summary>
    public class SerialPortDeviceCommunication : IDeviceCommunication, IDisposable
    {
        private readonly ILogger<SerialPortDeviceCommunication> _logger;

        /// <inheritdoc />
        public event Action<byte[]> DataReceived;

        /// <inheritdoc />
        public event Action<byte[]> DataSent;

        /// <inheritdoc />
        public event Action<ConnectionState> ConnectionStateChanged;

        public const byte DLE = 0x10;

        /// <summary>
        /// SerialPort DeviceCommunication
        /// </summary>
        /// <param name="comPort"></param>
        /// <param name="logger"></param>
        public SerialPortDeviceCommunication(
            string comPort,
            ILogger<SerialPortDeviceCommunication> logger = default)
        {
            this._logger = logger;
            throw new NotImplementedException("We currently only use network payment terminals");
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (disposing)
            {

            }
        }

        /// <inheritdoc />
        public bool IsConnected
        {
            get { return false; }
        }

        /// <inheritdoc />
        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                throw new NotImplementedException();
                //TODO: Add logic
                //return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                this._logger?.LogError($"{nameof(ConnectAsync)} - {exception}");
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                //TODO: Add logic
                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                this._logger?.LogError($"{nameof(DisconnectAsync)} - {exception}");
            }

            return Task.FromResult(false);
        }

        private void Disconnected()
        {
            this._logger?.LogInformation($"{nameof(Disconnected)}");

            this.ConnectionStateChanged?.Invoke(ConnectionState.Disconnected);
        }

        /// <inheritdoc />
        public async Task SendAsync(byte[] data)
        {
            var checksum = ChecksumHelper.CalcCrc2(data);
            var cs2 = new byte[] { (byte)(checksum >> 8), (byte)(checksum & 0xFF) };

            var tempData = new List<byte>();
            foreach (var b in data)
            {
                tempData.Add(b);

                if (b == DLE)
                {
                    tempData.Add(b);
                }
            }

            tempData.AddRange(cs2);

            var package = data.ToArray();
            this.DataSent?.Invoke(package);

            this._logger?.LogDebug($"{nameof(SendAsync)} - {BitConverter.ToString(package)}");
            //TODO: Add send logic
            throw new NotImplementedException();
        }

        private void Receive(object sender, byte[] data)
        {
            this._logger?.LogDebug($"{nameof(Receive)} - {BitConverter.ToString(data)}");
            this.DataReceived?.Invoke(data);
        }
    }
}

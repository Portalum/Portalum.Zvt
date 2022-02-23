using Microsoft.Extensions.Logging;
using Nager.TcpClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Portalum.Zvt
{
    /// <summary>
    /// TcpNetwork DeviceCommunication
    /// </summary>
    public class TcpNetworkDeviceCommunication : IDeviceCommunication, IDisposable
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly TcpClient _tcpClient;
        private readonly ILogger<TcpNetworkDeviceCommunication> _logger;

        /// <inheritdoc />
        public event Action<byte[]> DataReceived;

        /// <inheritdoc />
        public event Action<byte[]> DataSent;

        /// <inheritdoc />
        public event Action<ConnectionState> ConnectionStateChanged;

        /// <summary>
        /// TcpNetwork DeviceCommunication
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <param name="enableKeepAlive">Enable TcpKeepAlive</param>
        /// <param name="logger"></param>
        public TcpNetworkDeviceCommunication(
            string ipAddress,
            int port = 20007,
            bool enableKeepAlive = false,
            ILogger<TcpNetworkDeviceCommunication> logger = default)
        {
            this._ipAddress = ipAddress;
            this._port = port;

            if (enableKeepAlive)
            {
                var keepAliveConfig = new TcpClientKeepAliveConfig
                {
                    KeepAliveTime = 2,
                    KeepAliveInterval = 2,
                    KeepAliveRetryCount = 1
                };

                this._tcpClient = new TcpClient(keepAliveConfig: keepAliveConfig);
            }
            else
            {
                this._tcpClient = new TcpClient();
            }
            
            this._tcpClient.DataReceived += this.Receive;
            this._tcpClient.Connected += this.Connected;
            this._tcpClient.Disconnected += this.Disconnected;

            this._logger = logger;
        }

        /// <inheritdoc />
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
                if (this._tcpClient == null)
                {
                    return;
                }

                this._tcpClient.DataReceived -= this.Receive;
                this._tcpClient.Connected -= this.Connected;
                this._tcpClient.Disconnected -= this.Disconnected;

                if (this._tcpClient.IsConnected)
                {
                    this._tcpClient.Disconnect();
                }

                this._tcpClient.Dispose();
            }
        }

        /// <inheritdoc />
        public bool IsConnected
        {
            get { return this._tcpClient.IsConnected; }
        }

        /// <inheritdoc />
        public string ConnectionIdentifier
        {
            get { return this._ipAddress; }
        }

        /// <inheritdoc />
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                this._logger?.LogInformation($"{nameof(ConnectAsync)}");

#if NET6_0_OR_GREATER
                await this._tcpClient.ConnectAsync(this._ipAddress, this._port, cancellationToken);
#else
                await this._tcpClient.ConnectAsync(this._ipAddress, this._port);
#endif

                return true;
            }
            catch (Exception exception)
            {
                this._logger?.LogError($"{nameof(ConnectAsync)} - {exception}");
            }

            return false;
        }

        /// <inheritdoc />
        public Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                this._logger?.LogInformation($"{nameof(DisconnectAsync)}");
                this._tcpClient.Disconnect();
                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                this._logger?.LogError($"{nameof(DisconnectAsync)} - {exception}");
            }

            return Task.FromResult(false);
        }

        private void Connected()
        {
            this._logger?.LogInformation($"{nameof(Connected)}");

            this.ConnectionStateChanged?.Invoke(ConnectionState.Connected);
        }

        private void Disconnected()
        {
            this._logger?.LogInformation($"{nameof(Disconnected)}");

            this.ConnectionStateChanged?.Invoke(ConnectionState.Disconnected);
        }

        private void Receive(byte[] data)
        {
            this._logger?.LogDebug($"{nameof(Receive)} - {BitConverter.ToString(data)}");
            this.DataReceived?.Invoke(data);
        }

        /// <inheritdoc />
        public async Task SendAsync(byte[] data)
        {
            this.DataSent?.Invoke(data);

            this._logger?.LogDebug($"{nameof(SendAsync)} - {BitConverter.ToString(data)}");
            await this._tcpClient.SendAsync(data);
        }
    }
}

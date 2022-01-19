using Microsoft.Extensions.Logging;
using SimpleTcp;
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
        private readonly SimpleTcpClient _simpleTcpClient;
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
            this._simpleTcpClient = new SimpleTcpClient(ipAddress, port);
            this._simpleTcpClient.Events.DataReceived += this.Receive;
            this._simpleTcpClient.Events.Connected += this.Connected;
            this._simpleTcpClient.Events.Disconnected += this.Disconnected;

            if (enableKeepAlive)
            {
                this._simpleTcpClient.Keepalive = new SimpleTcpKeepaliveSettings
                {
                    EnableTcpKeepAlives = true,
                    TcpKeepAliveTime = 2,
                    TcpKeepAliveInterval = 2,
                    TcpKeepAliveRetryCount = 1
                };
            }

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
                if (this._simpleTcpClient == null)
                {
                    return;
                }

                this._simpleTcpClient.Events.DataReceived -= this.Receive;
                this._simpleTcpClient.Events.Connected -= this.Connected;
                this._simpleTcpClient.Events.Disconnected -= this.Disconnected;

                if (this._simpleTcpClient.IsConnected)
                {
                    this._simpleTcpClient.Disconnect();
                }

                this._simpleTcpClient.Dispose();
            }
        }

        /// <inheritdoc />
        public bool IsConnected
        {
            get { return this._simpleTcpClient.IsConnected; }
        }

        /// <inheritdoc />
        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                this._logger?.LogInformation($"{nameof(ConnectAsync)}");
                this._simpleTcpClient.Connect();
                return Task.FromResult(true);
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
                this._logger?.LogInformation($"{nameof(DisconnectAsync)}");
                this._simpleTcpClient.Disconnect();
                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                this._logger?.LogError($"{nameof(DisconnectAsync)} - {exception}");
            }

            return Task.FromResult(false);
        }

        private void Connected(object sender, ConnectionEventArgs e)
        {
            this._logger?.LogInformation($"{nameof(Connected)} {e.IpPort}");

            this.ConnectionStateChanged?.Invoke(ConnectionState.Connected);
        }

        private void Disconnected(object sender, ConnectionEventArgs e)
        {
            this._logger?.LogInformation($"{nameof(Disconnected)} {e.IpPort} {e.Reason}");

            this.ConnectionStateChanged?.Invoke(ConnectionState.Disconnected);
        }

        /// <inheritdoc />
        public async Task SendAsync(byte[] data)
        {
            this.DataSent?.Invoke(data);

            this._logger?.LogDebug($"{nameof(SendAsync)} - {BitConverter.ToString(data)}");
            await this._simpleTcpClient.SendAsync(data);
        }

        private void Receive(object sender, DataReceivedEventArgs e)
        {
            this._logger?.LogDebug($"{nameof(Receive)} - {BitConverter.ToString(e.Data)}");
            this.DataReceived?.Invoke(e.Data);
        }
    }
}

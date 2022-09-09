using System;
using System.Threading;
using System.Threading.Tasks;

namespace Portalum.Zvt
{
    /// <summary>
    /// Interface DeviceCommunication
    /// </summary>
    public interface IDeviceCommunication : IDisposable
    {
        /// <summary>
        /// Is the device connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The identifier for example the IpAddress or the SerialPort
        /// </summary>
        string ConnectionIdentifier { get; }

        /// <summary>
        /// On connection state changed
        /// </summary>
        event Action<ConnectionState> ConnectionStateChanged;

        /// <summary>
        /// On data received
        /// </summary>
        event Action<byte[]> DataReceived;

        /// <summary>
        /// On data sent
        /// </summary>
        event Action<byte[]> DataSent;

        /// <summary>
        /// Connect to the device
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnect the device
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Send data to the device
        /// </summary>
        /// <param name="data"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken = default);
    }
}

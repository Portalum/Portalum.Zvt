using System;
using System.Threading;
using System.Threading.Tasks;

namespace Portalum.Payment.Zvt
{
    public interface IDeviceCommunication
    {
        /// <summary>
        /// Is the device connected
        /// </summary>
        bool IsConnected { get; }

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
        /// <returns></returns>
        Task SendAsync(byte[] data);
    }
}

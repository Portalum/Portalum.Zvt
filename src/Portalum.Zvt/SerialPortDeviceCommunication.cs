using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Portalum.Zvt.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Portalum.Zvt
{
    /// <summary>
    /// SerialPort DeviceCommunication
    /// </summary>
    public class SerialPortDeviceCommunication : IDeviceCommunication
    {
        private readonly ILogger<SerialPortDeviceCommunication> _logger;
        private readonly string _portName;
        private readonly SerialPort _serialPort;
        private readonly List<byte> _receiveBuffer = new List<byte>();

        /// <inheritdoc />
        public event Action<byte[]> DataReceived;

        /// <inheritdoc />
        public event Action<byte[]> DataSent;

        /// <inheritdoc />
        public event Action<ConnectionState> ConnectionStateChanged;

        private const byte DLE = 0x10; //Data line escape
        private const byte STX = 0x02; //Start of text
        private const byte ETX = 0x03; //End of text
        private const byte ACK = 0x06; //Acknowledged
        private const byte NAK = 0x15; //Not acknowledged
        private const int MINIMUM_MESSAGE_SIZE = 7; //DLE + STX + APDU + DLE + ETX + CRC Checksum (2 bytes)
        private const int ETX_POSITION_INSIDE_MESSAGE = 3; //The third last byte
        private const int CHECKSUM_LENGTH = 2;

        /// <summary>
        /// SerialPort DeviceCommunication
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <param name="parity"></param>
        /// <param name="dataBits"></param>
        /// <param name="stopBits"></param>
        /// <param name="logger"></param>
        public SerialPortDeviceCommunication(
            string portName,
            int baudRate = 9600,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.Two,
            ILogger<SerialPortDeviceCommunication> logger = default)
        {
            this._portName = portName;

            if (logger == null)
            {
                logger = new NullLogger<SerialPortDeviceCommunication>();
            }
            this._logger = logger;

            this._serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            this._serialPort.DataReceived += this.Receive;
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
                this._serialPort.DataReceived -= this.Receive;
                this._serialPort.Dispose();
            }
        }

        /// <inheritdoc />
        public bool IsConnected
        {
            get { return this._serialPort.IsOpen; }
        }

        /// <inheritdoc />
        public string ConnectionIdentifier
        {
            get { return this._portName; }
        }

        /// <inheritdoc />
        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                this._logger.LogInformation($"{nameof(ConnectAsync)} - PortName:{this._portName}");

                this._serialPort.Open();

                return Task.FromResult(this._serialPort.IsOpen);
            }
            catch (Exception exception)
            {
                this._logger.LogError($"{nameof(ConnectAsync)} - {exception}");
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                this._serialPort.Close();
                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                this._logger.LogError($"{nameof(DisconnectAsync)} - {exception}");
            }

            return Task.FromResult(false);
        }

        private void Disconnected()
        {
            this._logger.LogInformation($"{nameof(Disconnected)}");

            this.ConnectionStateChanged?.Invoke(ConnectionState.Disconnected);
        }

        /// <inheritdoc />
        public async Task<bool> SendAsync(
            byte[] data,
            CancellationToken cancellationToken = default)
        {
            var checksum = ChecksumHelper.CalcCrc2(data);
            var cs2 = new byte[] { (byte)(checksum >> 8), (byte)(checksum & 0xFF) };

            using (var memoryStream = new MemoryStream())
            {
                #region Prepare data package

                memoryStream.WriteByte(DLE);
                memoryStream.WriteByte(STX);

                foreach (var b in data)
                {
                    memoryStream.WriteByte(b);

                    if (b == DLE)
                    {
                        memoryStream.WriteByte(b);
                    }
                }

                memoryStream.WriteByte(DLE);
                memoryStream.WriteByte(ETX);

                await memoryStream.WriteAsync(cs2, 0, cs2.Length, cancellationToken);

                #endregion

                var package = memoryStream.ToArray();

                #region Send and wait for Acknowledge

                this._serialPort.DataReceived -= this.Receive;
                this.SendRaw(package);
                while (!cancellationToken.IsCancellationRequested)
                {
                    //After a command always an acknowledge is send from the payment terminal device
                    var byte1 = (byte)this._serialPort.ReadByte();
                    if (byte1 == ACK)
                    {
                        this._logger.LogInformation($"{nameof(SendAsync)} - Acknowledge received");
                        break;
                    }
                }
                this._serialPort.DataReceived += this.Receive;

                #endregion
            }

            return true;
        }

        private void SendRaw(params byte[] data)
        {
            this.DataSent?.Invoke(data);

            if (this._logger.IsEnabled(LogLevel.Debug))
            {
                this._logger.LogDebug($"{nameof(SendRaw)} - {BitConverter.ToString(data)}");
            }

            this._serialPort.Write(data, 0, data.Length);         
        }

        private void Receive(object sender, SerialDataReceivedEventArgs e)
        {
            var buffer = new byte[this._serialPort.BytesToRead];

            this._serialPort.Read(buffer, 0, buffer.Length);

            this._logger.LogDebug($"{nameof(Receive)} - {BitConverter.ToString(buffer)}");

            this._receiveBuffer.AddRange(buffer);

            if (this._receiveBuffer.Count < MINIMUM_MESSAGE_SIZE ||
                this._receiveBuffer[this._receiveBuffer.Count - ETX_POSITION_INSIDE_MESSAGE] != ETX)
            {
                this._logger.LogDebug($"{nameof(Receive)} - Add to buffer");
                return;
            }

            var rawBufferData = this._receiveBuffer.ToArray();

            this._logger.LogDebug($"{nameof(Receive)} - Process buffer {BitConverter.ToString(rawBufferData)}");

            var cleanedData = new List<byte>();
            for (var i = 0; i < rawBufferData.Length; i++)
            {
                var b = rawBufferData[i];

                //Remove DLE STX start sequence
                if (i == 0)
                {
                    if (rawBufferData[i] == DLE && rawBufferData[i + 1] == STX)
                    {
                        i++;
                        continue;
                    }
                }

                //Check a next byte is available
                if ((i + 1) < rawBufferData.Length)
                {
                    //Remove DLE ETX sequence before checksum
                    if (rawBufferData[i] == DLE && rawBufferData[i + 1] == ETX)
                    {
                        i++;
                        continue;
                    }

                    //Remove second DLE
                    if (rawBufferData[i] == DLE && rawBufferData[i + 1] == DLE)
                    {
                        cleanedData.Add(b);
                        i++;
                        continue;
                    }
                }

                cleanedData.Add(b);
            }

            var receiveChecksum = cleanedData.Skip(cleanedData.Count - CHECKSUM_LENGTH);
            var receiveDataWithoutChecksum = cleanedData.Take(cleanedData.Count - CHECKSUM_LENGTH);

            var calculatedChecksum = ChecksumHelper.CalcCrc2(receiveDataWithoutChecksum);
            var cs2 = new byte[] { (byte)(calculatedChecksum >> 8), (byte)(calculatedChecksum & 0xFF) };

            if (Enumerable.SequenceEqual(cs2, receiveChecksum))
            {
                this.SendRaw(ACK);
                this._receiveBuffer.Clear();

                this.DataReceived?.Invoke(receiveDataWithoutChecksum.ToArray());
                return;
            }

            this._logger.LogWarning($"{nameof(Receive)} - Checksum invalid");

            this.SendRaw(NAK);
            this._receiveBuffer.Clear();
        }
    }
}

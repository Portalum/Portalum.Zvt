using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Portalum.Zvt.Helpers;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Portalum.Zvt
{
    /// <summary>
    /// SerialPort DeviceCommunication (Untested prototype)
    /// </summary>
    public class SerialPortDeviceCommunication : IDeviceCommunication
    {
        private readonly ILogger<SerialPortDeviceCommunication> _logger;
        private readonly string _comPort;
        private readonly SerialPort _serialPort;
        private readonly List<byte> _buffer = new List<byte>();

        /// <inheritdoc />
        public event Action<byte[]> DataReceived;

        /// <inheritdoc />
        public event Action<byte[]> DataSent;

        /// <inheritdoc />
        public event Action<ConnectionState> ConnectionStateChanged;

        private const byte DLE = 0x10; //Data line escape
        private const byte STX = 0x02; //Start of text
        private const byte ETX = 0x03; //End of text
        private const byte ACK = 0x06; //Acknowledge

        /// <summary>
        /// SerialPort DeviceCommunication
        /// </summary>
        /// <param name="comPort"></param>
        /// <param name="baudRate"></param>
        /// <param name="parity"></param>
        /// <param name="dataBits"></param>
        /// <param name="stopBits"></param>
        /// <param name="logger"></param>
        public SerialPortDeviceCommunication(
            string comPort,
            int baudRate,
            Parity parity,
            int dataBits,
            StopBits stopBits,
            ILogger<SerialPortDeviceCommunication> logger = default)
        {
            this._comPort = comPort;

            if (logger == null)
            {
                logger = new NullLogger<SerialPortDeviceCommunication>();
            }
            this._logger = logger;

            this._logger.LogInformation($"{nameof(SerialPortDeviceCommunication)} - This is an untested prototype");

            this._serialPort = new SerialPort(comPort, baudRate, parity, dataBits, stopBits);
            this._serialPort.ReceivedBytesThreshold = 2;
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
            get { return this._comPort; }
        }

        /// <inheritdoc />
        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                this._serialPort.Open();

                return Task.FromResult(true);
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
        public Task SendAsync(byte[] data)
        {
            var checksum = ChecksumHelper.CalcCrc2(data);
            var cs2 = new byte[] { (byte)(checksum >> 8), (byte)(checksum & 0xFF) };

            var tempData = new List<byte>();

            tempData.Add(DLE);
            tempData.Add(STX);

            foreach (var b in data)
            {
                tempData.Add(b);

                if (b == DLE)
                {
                    tempData.Add(b);
                }
            }

            tempData.Add(DLE);
            tempData.Add(ETX);

            tempData.AddRange(cs2);

            var package = tempData.ToArray();

            this.SendInternal(package);

            return Task.CompletedTask;
        }

        private void SendInternal(byte[] data, bool checkAcknowlege = true)
        {
            this.DataSent?.Invoke(data);

            this._logger.LogDebug($"{nameof(SendAsync)} - {BitConverter.ToString(data)}");

            if (checkAcknowlege)
            {
                this._serialPort.DataReceived -= this.Receive;
                this._serialPort.Write(data, 0, data.Length);
                while (true)
                {
                    //After a command always an acknowledge is send from the pt device
                    var byte1 = (byte)this._serialPort.ReadByte();
                    if (byte1 == ACK)
                    {
                        this._logger.LogInformation("Acknowledge received");
                        break;
                    }
                }
                this._serialPort.DataReceived += this.Receive;
            }
            else
            {
                this._serialPort.Write(data, 0, data.Length);
            }           
        }

        private void Receive(object sender, SerialDataReceivedEventArgs e)
        {
            var buffer = new byte[this._serialPort.BytesToRead];

            this._serialPort.Read(buffer, 0, buffer.Length);

            this._logger.LogDebug($"{nameof(Receive)} - {BitConverter.ToString(buffer)}");

            this._buffer.AddRange(buffer);

            if (this._buffer.Count < 3 || this._buffer[this._buffer.Count - 3] != ETX)
            {
                this._logger.LogDebug($"{nameof(Receive)} - Add to buffer");
                return;
            }

            var rawBufferData = this._buffer.ToArray();

            this._logger.LogDebug($"{nameof(Receive)} - Process buffer {BitConverter.ToString(rawBufferData)}");

            //TODO: Send acknowledge only if checksum valid
            var acknowledge = new byte[] { ACK };
            this.SendInternal(acknowledge, checkAcknowlege: false);

            var cleanData = new List<byte>();
            for (var i = 0; i < rawBufferData.Length; i++)
            {
                var b = rawBufferData[i];

                if (i == 0)
                {
                    if (rawBufferData[0] == DLE && rawBufferData[1] == STX)
                    {
                        i++;
                        continue;
                    }
                }

                if (rawBufferData[i] == DLE && rawBufferData[i+1] == ETX)
                {
                    i++;
                    continue;
                }

                if (rawBufferData[i] == DLE && rawBufferData[i + 1] == DLE)
                {
                    cleanData.Add(b);
                    i++;
                    continue;
                }

                cleanData.Add(b);
            }

            this.DataReceived?.Invoke(cleanData.Take(cleanData.Count - 2).ToArray());
            this._buffer.Clear();
        }
    }
}

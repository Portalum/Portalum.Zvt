using Microsoft.Extensions.Logging;
using Portalum.Zvt.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Portalum.Zvt.Helpers;
using System.Collections.Generic;

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
        private readonly SemaphoreSlim _processingSyncLock = new SemaphoreSlim(1);

        private CancellationTokenSource _commandCompletionCancellationTokenSource;
        private byte[] _dataBuffer;
        private bool _waitForCommandCompletion = false;
        private bool _transactionActive = false;

        /// <summary>
        /// New data received from the pt device
        /// </summary>
        public event Func<byte[], ProcessData> DataReceived;

        /// <summary>
        /// A callback which is checked 
        /// </summary>
        public event Func<CompletionInfo> GetCompletionInfo;

        private readonly byte[] _positiveCompletionData1 = new byte[] { 0x80, 0x00, 0x00 }; //Default
        private readonly byte[] _positiveCompletionData2 = new byte[] { 0x84, 0x00, 0x00 }; //Alternative
        private readonly byte[] _positiveCompletionData3 = new byte[] { 0x84, 0x9C, 0x00 }; //Special case for request more time
        private readonly byte[] _negativeIssueGoodsData = new byte[] { 0x84, 0x66, 0x00 };
        private readonly byte[] _otherCommandData = new byte[] { 0x84, 0x83, 0x00 };
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
            this._deviceCommunication.DataReceived += this.DataReceiveSwitch;
        }

        /// <inheritdoc />
        public virtual void Dispose()
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
                this._deviceCommunication.DataReceived -= this.DataReceiveSwitch;
            }
        }

        public void TransactionActive()
        {
            this._transactionActive = true;
        }

        public void TransactionInactive()
        {
            this._transactionActive = false;
        }

        /// <summary>
        /// Switch for incoming data
        /// </summary>
        /// <param name="data"></param>
        protected virtual void DataReceiveSwitch(byte[] data)
        {
            try
            {
                this._processingSyncLock.Wait();

                if (this._waitForCommandCompletion)
                {
                    this._logger.LogDebug($"{nameof(DataReceiveSwitch)} - wait for Command Completion");

                    this._dataBuffer = data;
                    this._waitForCommandCompletion = false;

                    try
                    {
                        this._commandCompletionCancellationTokenSource?.Cancel();
                    }
                    catch (ObjectDisposedException)
                    {
                        this._logger.LogWarning($"{nameof(DataReceiveSwitch)} - TokenSource is already disposed");
                    }

                    return;
                }
            }
            finally
            {
                this._processingSyncLock.Release();
            }

            this.ProcessData(data);
        }

        /// <summary>
        /// Process received data and respond according to the ZVT protocol and / or the current state
        /// This method acts as the responder on the zvt protocol level. If you need to respond differently
        /// then you can override this method and implement your own logic or catch certain cases.
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual void ProcessData(byte[] data)
        {
            var dataProcessed = this.DataReceived?.Invoke(data);
            if (dataProcessed == null)
            {
                this._logger.LogError($"{nameof(ProcessData)} - dataProcessed is null");
                return;
            }

            switch (dataProcessed.State)
            {
                case ProcessDataState.WaitForMoreData:
                    return;
                case ProcessDataState.CannotProcess:
                case ProcessDataState.ParseFailure:
                    this._logger.LogError($"{nameof(ProcessData)} - State:{dataProcessed.State} {BitConverter.ToString(data)}");
                    return;
                case ProcessDataState.Processed:
                    break;
                default:
                    this._logger.LogError($"{nameof(ProcessData)} - Unknown State: {dataProcessed.State}");
                    return;
            }

            if (!this._transactionActive)
            {
                this._logger.LogInformation($"{nameof(ProcessData)} - Receive data in transaction inactive state");
                this._deviceCommunication.SendAsync(this._negativeIssueGoodsData);
                return;
            }

            // Is StatusInformation and ErrorCode is 0
            if (dataProcessed.Response is StatusInformation { ErrorCode: 0 })
            {
                var completionInfo = this.GetCompletionInfo?.Invoke();
                if (completionInfo == null)
                {
                    //Default if no one has subscribed to the event, immediately approve the transaction
                    this._deviceCommunication.SendAsync(this._positiveCompletionData1);
                }
                else
                {
                    switch (completionInfo.State)
                    {
                        case CompletionInfoState.Wait:
                            this._deviceCommunication.SendAsync(this._positiveCompletionData3);
                            break;
                        case CompletionInfoState.ChangeAmount:
                            var controlField = new byte[] { 0x84, 0x9D };

                            // Change the amount from the original in the start request
                            var package = new List<byte>();
                            package.Add(0x04); //Amount prefix
                            package.AddRange(NumberHelper.DecimalToBcd(completionInfo.Amount));
                            this._deviceCommunication.SendAsync(PackageHelper.Create(controlField, package.ToArray()));
                            break;
                        case CompletionInfoState.Successful:
                            this._deviceCommunication.SendAsync(this._positiveCompletionData1);
                            break;
                        case CompletionInfoState.Failure:
                            this._deviceCommunication.SendAsync(this._negativeIssueGoodsData);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
            else if (dataProcessed.Response is StatusInformation statusInformation)
            {
                this._logger.LogInformation($"{nameof(ProcessData)} - StatusInformation with ErrorCode:{statusInformation.ErrorCode:X2} {statusInformation.ErrorMessage} received");
                this._deviceCommunication.SendAsync(this._positiveCompletionData1);
            }
            else if (dataProcessed.Response is Completion completion)
            {
                this._deviceCommunication.SendAsync(this._positiveCompletionData1);
                return;
            }
            else if (dataProcessed.Response is Abort abort)
            {
                this._deviceCommunication.SendAsync(this._positiveCompletionData1);
                return;
            }
            else if (dataProcessed.Response is IntermediateStatusInformation intermediateStatusInformation)
            {
                this._deviceCommunication.SendAsync(this._positiveCompletionData1);
                return;
            }
            else if (dataProcessed.Response is PrintLineInfo printLineInfo)
            {
                this._deviceCommunication.SendAsync(this._positiveCompletionData1);
                return;
            }
            else if (dataProcessed.Response is PrintTextBlock printTextBlock)
            {
                this._deviceCommunication.SendAsync(this._positiveCompletionData1);
                return;
            }
            else
            {
                this._logger.LogError($"{nameof(ProcessData)} - Response is not StatusInformation");
                this._deviceCommunication.SendAsync(this._negativeIssueGoodsData);
            }
        }

        /// <summary>
        /// Send command
        /// </summary>
        /// <param name="commandData">The data of the command</param>
        /// <param name="commandCompletionReceiveTimeoutMilliseconds">Maximum waiting time for the command completion package, default is 5 seconds, T3 Timeout</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async virtual Task<SendCommandResult> SendCommandAsync(
            byte[] commandData,
            int commandCompletionReceiveTimeoutMilliseconds = 5000,
            CancellationToken cancellationToken = default)
        {
            this.ResetDataBuffer();

            this._commandCompletionCancellationTokenSource?.Dispose();
            this._commandCompletionCancellationTokenSource = new CancellationTokenSource();

            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this._commandCompletionCancellationTokenSource.Token);

            this._waitForCommandCompletion = true;
            try
            {
                await this._deviceCommunication.SendAsync(commandData, linkedCancellationTokenSource.Token).ContinueWith(task => { });
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, $"{nameof(SendCommandAsync)} - Cannot send data");
                this._commandCompletionCancellationTokenSource.Dispose();
                return SendCommandResult.SendFailure;
            }

            await Task.Delay(commandCompletionReceiveTimeoutMilliseconds, linkedCancellationTokenSource.Token).ContinueWith(task =>
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    this._logger.LogError($"{nameof(SendCommandAsync)} - Wait task for command completion was aborted");
                }
            });

            this._commandCompletionCancellationTokenSource.Dispose();

            if (this._dataBuffer == null)
            {
                return SendCommandResult.NoDataReceived;
            }

            if (this.CheckIsPositiveCompletion())
            {
                this.ForwardUnusedBufferData();

                return SendCommandResult.PositiveCompletionReceived;
            }

            if (this.CheckIsNotSupported())
            {
                return SendCommandResult.NotSupported;
            }

            if (this.CheckIsNegativeCompletion())
            {
                this._logger.LogError($"{nameof(SendCommandAsync)} - 'Negative completion' received");
                return SendCommandResult.NegativeCompletionReceived;
            }

            this._logger.LogError($"{nameof(SendCommandAsync)} - Unknown Failure, DataBuffer {BitConverter.ToString(this._dataBuffer)}");
            return SendCommandResult.UnknownFailure;
        }

        /// <summary>
        /// Check if the received data indicates a positive command completion
        /// </summary>
        protected virtual bool CheckIsPositiveCompletion()
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

        /// <summary>
        /// Check if the received data indicates a negative command completion
        /// </summary>
        protected virtual bool CheckIsNegativeCompletion()
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

        /// <summary>
        /// Check if the received data indicates a "command not supported" or "unknown" command response from the PT
        /// </summary>
        protected virtual bool CheckIsNotSupported()
        {
            if (this._dataBuffer.Length < 3)
            {
                return false;
            }

            var buffer = this._dataBuffer.AsSpan().Slice(0, 3);

            if (buffer.SequenceEqual(this._otherCommandData))
            {
                return true;
            }

            return false;
        }

        private void ResetDataBuffer()
        {
            this._dataBuffer = null;
        }

        private void ForwardUnusedBufferData()
        {
            if (this._dataBuffer.Length == 3)
            {
                this.ResetDataBuffer();
                return;
            }

            var unusedData = this._dataBuffer.AsSpan().Slice(3).ToArray();
            this.ProcessData(unusedData);
        }
    }
}

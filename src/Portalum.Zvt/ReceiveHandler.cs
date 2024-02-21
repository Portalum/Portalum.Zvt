using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Portalum.Zvt.Helpers;
using Portalum.Zvt.Models;
using Portalum.Zvt.Parsers;
using Portalum.Zvt.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Portalum.Zvt
{
    /// <summary>
    /// ReceiveHandler
    /// </summary>
    public class ReceiveHandler : IReceiveHandler
    {
        private readonly ILogger _logger;
        private readonly IErrorMessageRepository _errorMessageRepository;

        private readonly IPrintLineParser _printLineParser;
        private readonly IPrintTextBlockParser _printTextBlockParser;
        private readonly IStatusInformationParser _statusInformationParser;
        private readonly IIntermediateStatusInformationParser _intermediateStatusInformationParser;
        private readonly List<byte[]> _availableControlFields;

        private readonly byte[] _receiveBuffer = new byte[ushort.MaxValue];
        private int _receiveBufferEndPosition = 0;
        private int _missingDataOfExpectedPackage = 0;

        private readonly byte[] _statusInformationControlField = new byte[] { 0x04, 0x0F };
        private readonly byte[] _intermediateStatusInformationControlField = new byte[] { 0x04, 0xFF };
        private readonly byte[] _printLineControlField = new byte[] { 0x06, 0xD1 };
        private readonly byte[] _printTextBlockControlField = new byte[] { 0x06, 0xD3 };
        private readonly byte[] _completionCommandControlField = new byte[] { 0x06, 0x0F };
        private readonly byte[] _abortCommandControlField = new byte[] { 0x06, 0x1E };

        /// <inheritdoc />
        public event Action<PrintLineInfo> LineReceived;

        /// <inheritdoc />
        public event Action<ReceiptInfo> ReceiptReceived;

        /// <inheritdoc />
        public event Action<StatusInformation> StatusInformationReceived;

        /// <inheritdoc />
        public event Action<byte, string> IntermediateStatusInformationReceived;

        /// <inheritdoc />
        public event Action<byte[]> CompletionReceived;

        /// <inheritdoc />
        public event Action<byte, string> AbortReceived;

        /// <summary>
        /// ReceiveHandler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="encoding"></param>
        /// <param name="errorMessageRepository"></param>
        /// <param name="intermediateStatusRepository"></param>
        /// <param name="printLineParser"></param>
        /// <param name="printTextBlockParser"></param>
        /// <param name="statusInformationParser"></param>
        /// <param name="intermediateStatusInformationParser"></param>
        public ReceiveHandler(
            ILogger logger,
            Encoding encoding,
            IErrorMessageRepository errorMessageRepository,
            IIntermediateStatusRepository intermediateStatusRepository,
            IPrintLineParser printLineParser = default,
            IPrintTextBlockParser printTextBlockParser = default,
            IStatusInformationParser statusInformationParser = default,
            IIntermediateStatusInformationParser intermediateStatusInformationParser = default)
        {
            this._logger = logger;
            this._errorMessageRepository = errorMessageRepository;

            this._printLineParser = printLineParser == default
                ? new PrintLineParser(logger, encoding)
                : printLineParser;

            this._printTextBlockParser = printTextBlockParser == default
                ? new PrintTextBlockParser(logger, encoding, errorMessageRepository)
                : printTextBlockParser;

            this._statusInformationParser = statusInformationParser == default
                ? new StatusInformationParser(logger, encoding, errorMessageRepository)
                : statusInformationParser;

            this._intermediateStatusInformationParser = intermediateStatusInformationParser == default
                ? new IntermediateStatusInformationParser(logger, encoding, intermediateStatusRepository, errorMessageRepository)
                : intermediateStatusInformationParser;

            this._availableControlFields = new List<byte[]>
            {
                this._statusInformationControlField,
                this._intermediateStatusInformationControlField,
                this._printLineControlField,
                this._printTextBlockControlField,
                this._completionCommandControlField,
                this._abortCommandControlField
            };
        }

        /// <inheritdoc />
        public virtual ProcessData ProcessData(Span<byte> data)
        {
            if (data.Length == 0)
            {
                return new ProcessData { State = ProcessDataState.CannotProcess };
            }

            var apduInfo = ApduHelper.GetApduInfo(data);

            Span<byte> apduData = null;

            #region Complete apdu package

            if (apduInfo.PackageSize == data.Length)
            {
                if (this._availableControlFields.Any(controlField => Enumerable.SequenceEqual(apduInfo.ControlField, controlField)))
                {
                    this.ResetFragmentInfo();
                    apduData = data.Slice(apduInfo.DataStartIndex, apduInfo.DataLength);
                    return this.ProcessApdu(apduInfo, apduData);
                }
            }

            #endregion

            #region Fragmented apdu package

            if (this._missingDataOfExpectedPackage == 0)
            {
                if (data.Length < apduInfo.PackageSize)
                {
                    this._logger.LogTrace($"{nameof(ProcessData)} - Apdu first data part fragment");

                    data.CopyTo(this._receiveBuffer.AsSpan().Slice(this._receiveBufferEndPosition));

                    this._receiveBufferEndPosition = data.Length;
                    this._missingDataOfExpectedPackage = apduInfo.PackageSize - this._receiveBufferEndPosition;

                    return new ProcessData { State = ProcessDataState.WaitForMoreData };
                }

                if (data.Length > apduInfo.PackageSize)
                {
                    this.ResetFragmentInfo();
                    this._logger.LogError($"{nameof(ProcessData)} - Apdu data part corrupt");
                    return new ProcessData { State = ProcessDataState.CannotProcess };
                }

                apduData = data.Slice(apduInfo.DataStartIndex, apduInfo.DataLength);
            }
            else
            {
                apduInfo = ApduHelper.GetApduInfo(this._receiveBuffer);

                if (this._missingDataOfExpectedPackage > 0)
                {
                    this._logger.LogTrace($"{nameof(ProcessData)} - Apdu additional data part fragment");

                    data.CopyTo(this._receiveBuffer.AsSpan().Slice(this._receiveBufferEndPosition));

                    this._receiveBufferEndPosition += data.Length;
                    this._missingDataOfExpectedPackage = apduInfo.PackageSize - this._receiveBufferEndPosition;

                    if (this._missingDataOfExpectedPackage > 0)
                    {
                        return new ProcessData { State = ProcessDataState.WaitForMoreData };
                    }
                }

                this._logger.LogTrace($"{nameof(ProcessData)} - Apdu all data part fragments");
                using (var memoryStream = new MemoryStream())
                {
                    memoryStream.Write(this._receiveBuffer, 0, this._receiveBufferEndPosition);
                    memoryStream.Write(data.ToArray(), 0, data.Length);

                    var fullData = memoryStream.ToArray().AsSpan();
                    if (fullData.Length >= apduInfo.PackageSize)
                    {
                        apduData = fullData.Slice(apduInfo.DataStartIndex, apduInfo.DataLength);
                    }
                }
            }

            #endregion

            this.ResetFragmentInfo();
            return this.ProcessApdu(apduInfo, apduData);
        }

        private void ResetFragmentInfo()
        {
            this._receiveBufferEndPosition = 0;
            this._missingDataOfExpectedPackage = 0;
        }

        /// <summary>
        /// Process a received application process data unit (APDU) using one of the available parsers.
        /// This method also fires any event handler for the message type.
        /// </summary>
        /// <param name="apduInfo">Parsed APDU info from the message header</param>
        /// <param name="apduData">The raw data of the message (including the message header)</param>
        /// <returns></returns>
        protected virtual ProcessData ProcessApdu(
            ApduResponseInfo apduInfo,
            Span<byte> apduData)
        {
            if (apduInfo.ControlField == null ||
                apduInfo.ControlField.Length != 2)
            {
                return new ProcessData { State = ProcessDataState.ParseFailure };
            }

            //Status Information
            if (apduInfo.CanHandle(this._statusInformationControlField))
            {
                var statusInformation = this._statusInformationParser.Parse(apduData);
                if (statusInformation == null)
                {
                    return new ProcessData { State = ProcessDataState.ParseFailure };
                }

                this.StatusInformationReceived?.Invoke(statusInformation);
                return new ProcessData { State = ProcessDataState.Processed, Response = statusInformation };
            }

            //Intermediate Status Information
            if (apduInfo.CanHandle(this._intermediateStatusInformationControlField))
            {
                var intermediateStatusInformation = this._intermediateStatusInformationParser.GetMessage(apduData);
                if (intermediateStatusInformation.StatusInformation == null)
                {
                    return new ProcessData { State = ProcessDataState.ParseFailure };
                }

                this.IntermediateStatusInformationReceived?.Invoke(intermediateStatusInformation.StatusCode, intermediateStatusInformation.StatusInformation);
                return new ProcessData
                {
                    State = ProcessDataState.Processed,
                    Response = new IntermediateStatusInformation { ErrorMessage = intermediateStatusInformation.StatusInformation }
                };
            }

            //Print Line
            if (apduInfo.CanHandle(this._printLineControlField))
            {
                //Use apdu length info, length is hardcoded
                var printLineInfo = this._printLineParser.Parse(apduData);
                this.LineReceived?.Invoke(printLineInfo);
                return new ProcessData { State = ProcessDataState.Processed, Response = printLineInfo };
            }

            //Print Text-Block
            if (apduInfo.CanHandle(this._printTextBlockControlField))
            {
                var receipt = this._printTextBlockParser.Parse(apduData);
                if (receipt == null)
                {
                    return new ProcessData { State = ProcessDataState.ParseFailure };
                }

                this.ReceiptReceived?.Invoke(receipt);
                return new ProcessData { State = ProcessDataState.Processed, Response = new PrintTextBlock() };
            }

            //Completion (3.2 Completion)
            if (apduInfo.CanHandle(this._completionCommandControlField))
            {
                if (apduData.Length == 0)
                {
                    this._logger.LogDebug($"{nameof(ProcessApdu)} - 'Completion' received, data:{ByteHelper.ByteArrayToHex(apduData, prettify: true)}");
                    this.CompletionReceived?.Invoke(Array.Empty<byte>());
                }
                else
                {
                    this._logger.LogDebug($"{nameof(ProcessApdu)} - 'Completion' received");
                    this.CompletionReceived?.Invoke(apduData.ToArray());
                }

                return new ProcessData { State = ProcessDataState.Processed, Response = new Completion() };
            }

            //Abort (3.3 Abort)
            if (apduInfo.CanHandle(this._abortCommandControlField))
            {
                (byte StatusCode, string StatusInformation) errorMessage = (0, string.Empty);
                byte errorCode = 0x60;

                if (apduData.Length > 0)
                {
                    errorCode = apduData[0];
                    errorMessage = this._errorMessageRepository.GetMessage(errorCode);
                }
                else
                {
                    errorMessage.StatusInformation = "Cannot detect error code";
                }

                this._logger.LogDebug($"{nameof(ProcessApdu)} - 'Abort' received with message:{errorMessage}");
                this.AbortReceived?.Invoke(errorMessage.StatusCode, errorMessage.StatusInformation);

                return new ProcessData
                {
                    State = ProcessDataState.Processed,
                    Response = new Abort { ErrorCode = errorCode, ErrorMessage = errorMessage.StatusInformation }
                };
            }

            return new ProcessData { State = ProcessDataState.CannotProcess };
        }
    }
}

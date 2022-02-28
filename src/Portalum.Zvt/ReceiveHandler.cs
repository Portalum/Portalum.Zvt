using Microsoft.Extensions.Logging;
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

        private byte[] _receiveBuffer = new byte[10000];
        private int _receiveBufferEndPosition = 0;
        private int _missingDataOfExpectedPackage = 0;

        private readonly byte[] _statusInformationControlField = new byte[] { 0x04, 0x0F };
        private readonly byte[] _intermediateStatusInformationControlField = new byte[] { 0x04, 0xFF };
        private readonly byte[] _printLineControlField = new byte[] { 0x06, 0xD1 };
        private readonly byte[] _printTextBlockControlField = new byte[] { 0x06, 0xD3 };
        private readonly byte[] _otherCommandControlField = new byte[] { 0x84, 0x83 };
        private readonly byte[] _completionCommandControlField = new byte[] { 0x06, 0x0F };
        private readonly byte[] _abortCommandControlField = new byte[] { 0x06, 0x1E };

        /// <inheritdoc />
        public event Action<PrintLineInfo> LineReceived;

        /// <inheritdoc />
        public event Action<ReceiptInfo> ReceiptReceived;

        /// <inheritdoc />
        public event Action<StatusInformation> StatusInformationReceived;

        /// <inheritdoc />
        public event Action<string> IntermediateStatusInformationReceived;

        /// <inheritdoc />
        public event Action CompletionReceived;

        /// <inheritdoc />
        public event Action<string> AbortReceived;

        /// <inheritdoc />
        public event Action NotSupportedReceived;

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
                this._otherCommandControlField,
                this._completionCommandControlField,
                this._abortCommandControlField
            };
        }

        /// <inheritdoc />
        public bool ProcessData(Span<byte> data)
        {
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

                    return true;
                }

                if (data.Length > apduInfo.PackageSize)
                {
                    this._logger.LogError($"{nameof(ProcessData)} - Apdu data part corrupt");
                    return false;
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
                        return true;
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

        private bool ProcessApdu(
            ApduResponseInfo apduInfo,
            Span<byte> apduData)
        {
            if (apduInfo.ControlField == null ||
                apduInfo.ControlField.Length != 2)
            {
                return false;
            }

            //Status Information
            if (apduInfo.CanHandle(this._statusInformationControlField))
            {
                var statusInformation = this._statusInformationParser.Parse(apduData);
                this.StatusInformationReceived?.Invoke(statusInformation);
                return true;
            }

            //Intermediate Status Information
            if (apduInfo.CanHandle(this._intermediateStatusInformationControlField))
            {
                var intermediateStatusInformation = this._intermediateStatusInformationParser.GetMessage(apduData);
                this.IntermediateStatusInformationReceived?.Invoke(intermediateStatusInformation);
                return true;
            }

            //Print Line
            if (apduInfo.CanHandle(this._printLineControlField))
            {
                //Use apdu length info, length is hardcoded
                var printLineInfo = this._printLineParser.Parse(apduData);
                this.LineReceived?.Invoke(printLineInfo);
                return true;
            }

            //Print Text-Block
            if (apduInfo.CanHandle(this._printTextBlockControlField))
            {
                var receipt = this._printTextBlockParser.Parse(apduData);
                this.ReceiptReceived?.Invoke(receipt);
                return true;
            }

            //Command not supported (2.67 Other Commands)
            if (apduInfo.CanHandle(this._otherCommandControlField))
            {
                this._logger.LogDebug($"{nameof(ProcessApdu)} - 'Command not supported' received");
                this.NotSupportedReceived?.Invoke();
                return true;
            }

            //Completion (3.2 Completion)
            if (apduInfo.CanHandle(this._completionCommandControlField))
            {
                this._logger.LogDebug($"{nameof(ProcessApdu)} - 'Completion' received");
                this.CompletionReceived?.Invoke();
                return true;
            }

            //Abort (3.3 Abort)
            if (apduInfo.CanHandle(this._abortCommandControlField))
            {
                var errorCode = apduData[0];
                var errorMessage = this._errorMessageRepository.GetMessage(errorCode);

                this._logger.LogDebug($"{nameof(ProcessApdu)} - 'Abort' received with message:{errorMessage}");
                this.AbortReceived?.Invoke(errorMessage);

                return true;
            }

            return false;
        }
    }
}

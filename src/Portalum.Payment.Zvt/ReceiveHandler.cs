using Microsoft.Extensions.Logging;
using Portalum.Payment.Zvt.Helpers;
using Portalum.Payment.Zvt.Models;
using Portalum.Payment.Zvt.Parsers;
using Portalum.Payment.Zvt.Repositories;
using System;

namespace Portalum.Payment.Zvt
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
        /// <param name="errorMessageRepository"></param>
        /// <param name="printLineParser"></param>
        /// <param name="printTextBlockParser"></param>
        /// <param name="statusInformationParser"></param>
        /// <param name="intermediateStatusInformationParser"></param>
        public ReceiveHandler(
            ILogger logger,
            IErrorMessageRepository errorMessageRepository,
            IPrintLineParser printLineParser = default,
            IPrintTextBlockParser printTextBlockParser = default,
            IStatusInformationParser statusInformationParser = default,
            IIntermediateStatusInformationParser intermediateStatusInformationParser = default)
        {
            this._logger = logger;
            this._errorMessageRepository = errorMessageRepository;

            this._printLineParser = printLineParser == default
                ? new PrintLineParser(logger)
                : printLineParser;

            this._printTextBlockParser = printTextBlockParser == default
                ? new PrintTextBlockParser(logger, errorMessageRepository)
                : printTextBlockParser;

            this._statusInformationParser = statusInformationParser == default
                ? new StatusInformationParser(logger, errorMessageRepository)
                : statusInformationParser;

            this._intermediateStatusInformationParser = intermediateStatusInformationParser == default
                ? new IntermediateStatusInformationParser(logger)
                : intermediateStatusInformationParser;
        }

        /// <inheritdoc />
        public bool ProcessData(Span<byte> data)
        {
            var apduInfo = ApduHelper.GetApduInfo(data);
            if (apduInfo.ControlField == null)
            {
                return false;
            }

            if (data.Length != apduInfo.DataStartIndex + apduInfo.DataLength)
            {
                this._logger.LogError($"{nameof(ProcessData)} - Apdu data part corrupt");
                return false;
            }

            var apduData = data.Slice(apduInfo.DataStartIndex, apduInfo.DataLength);

            //Status Information
            if (apduInfo.CanHandle(0x04, 0x0F))
            {
                var statusInformation = this._statusInformationParser.Parse(apduData);
                this.StatusInformationReceived?.Invoke(statusInformation);
                return true;
            }

            //Intermediate Status Information
            if (apduInfo.CanHandle(0x04, 0xFF))
            {
                var intermediateStatusInformation = this._intermediateStatusInformationParser.GetMessage(data);
                this.IntermediateStatusInformationReceived?.Invoke(intermediateStatusInformation);
                return true;
            }

            //Print Line
            if (apduInfo.CanHandle(0x06, 0xD1))
            {
                //Use apdu length info, length is hardcoded
                var printLineInfo = this._printLineParser.Parse(apduData);
                this.LineReceived?.Invoke(printLineInfo);
                return true;
            }

            //Print Text-Block
            if (apduInfo.CanHandle(0x06, 0xD3))
            {
                var receipt = this._printTextBlockParser.Parse(apduData);
                this.ReceiptReceived?.Invoke(receipt);
                return true;
            }

            //Command not supported (2.67 Other Commands)
            if (apduInfo.CanHandle(0x84, 0x83))
            {
                this._logger.LogDebug($"{nameof(ProcessData)} - 'Command not supported' received");
                this.NotSupportedReceived?.Invoke();
                return true;
                //TODO: Process this event and return an other error
            }

            //Completion (3.2 Completion)
            if (apduInfo.CanHandle(0x06, 0x0F))
            {
                this._logger.LogDebug($"{nameof(ProcessData)} - 'Completion' received");
                this.CompletionReceived?.Invoke();
                return true;
            }

            //Abort (3.3 Abort)
            if (apduInfo.CanHandle(0x06, 0x1E))
            {
                var errorCode = apduData[0];
                var errorMessage = this._errorMessageRepository.GetMessage(errorCode);

                this._logger.LogDebug($"{nameof(ProcessData)} - 'Abort' received {errorMessage}");
                this.AbortReceived?.Invoke(errorMessage);

                return true;
            }

            return false;
        }
    }
}

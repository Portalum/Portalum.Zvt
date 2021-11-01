using Microsoft.Extensions.Logging;
using Portalum.Payment.Zvt.Models;
using Portalum.Payment.Zvt.Parsers;
using Portalum.Payment.Zvt.Repositories;
using System;

namespace Portalum.Payment.Zvt
{
    public class ReceiveHandler
    {
        private readonly ILogger _logger;
        private readonly IErrorMessageRepository _errorMessageRepository;
        private readonly int _controlFieldLength = 2;
        private readonly byte _extendedLengthFieldIndicator = 0xFF;
        private readonly byte _extendedLengthFieldByteCount = 2;

        private readonly PrintLineParser _printLineParser;
        private readonly PrintTextBlockParser _printTextBlockParser;
        private readonly StatusInformationParser _statusInformationParser;
        private readonly IntermediateStatusInformationParser _intermediateStatusInformationParser;

        public event Action<PrintLineInfo> LineReceived;
        public event Action<ReceiptInfo> ReceiptReceived;
        public event Action<StatusInformation> StatusInformationReceived;
        public event Action<string> IntermediateStatusInformationReceived;

        public event Action CompletionReceived;
        public event Action<string> AbortReceived;
        public event Action NotSupportedReceived;

        public ReceiveHandler(
            ILogger logger,
            IErrorMessageRepository errorMessageRepository)
        {
            this._logger = logger;
            this._errorMessageRepository = errorMessageRepository;

            this._printLineParser = new PrintLineParser(logger);
            this._printTextBlockParser = new PrintTextBlockParser(logger, errorMessageRepository);
            this._statusInformationParser = new StatusInformationParser(logger, errorMessageRepository);
            this._intermediateStatusInformationParser = new IntermediateStatusInformationParser(logger);
        }

        public ApduResponseInfo GetApduInfo(Span<byte> data)
        {
            if (data.Length < 3)
            {
                // More than 2 bytes required
                //
                // 00-00-00
                // |  |  |  
                // │  │  └─ Length
                // │  └─ Control field INSTR
                // └─ Control field CLASS

                this._logger.LogError($"{nameof(GetApduInfo)} - Receive data packet that is too short");
                return new ApduResponseInfo();
            }

            var apduDefaultLengthByteCount = 1;

            var item = new ApduResponseInfo();
            item.ControlField = data.Slice(0, this._controlFieldLength).ToArray();

            var packageData = data.Slice(this._controlFieldLength, 1);
            var startIndex = this._controlFieldLength + apduDefaultLengthByteCount;

            if (packageData[0] != this._extendedLengthFieldIndicator)
            {
                item.DataLength = packageData[0];
                item.DataStartIndex = startIndex;
            }
            else
            {
                item.DataLength = BitConverter.ToInt16(data.Slice(startIndex, this._extendedLengthFieldByteCount));
                item.DataStartIndex = startIndex + this._extendedLengthFieldByteCount;
            }

            return item;
        }

        public bool ProcessData(Span<byte> data)
        {
            var apduInfo = this.GetApduInfo(data);
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

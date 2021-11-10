using Microsoft.Extensions.Logging;
using Portalum.Payment.Zvt.Models;
using Portalum.Payment.Zvt.Repositories;
using Portalum.Payment.Zvt.Responses;
using System;
using System.Text;

namespace Portalum.Payment.Zvt.Parsers
{
    /// <summary>
    /// IntermediateStatusInformationParser
    /// </summary>
    public class IntermediateStatusInformationParser : IIntermediateStatusInformationParser
    {
        private readonly ILogger _logger;
        private readonly IIntermediateStatusRepository _intermediateStatusRepository;
        private readonly BmpParser _bmpParser;
        private readonly TlvParser _tlvParser;

        private readonly StringBuilder _tlvTextContent;

        /// <summary>
        /// IntermediateStatusInformationParser
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="intermediateStatusRepository"></param>
        /// <param name="errorMessageRepository"></param>
        public IntermediateStatusInformationParser(
            ILogger logger,
            IIntermediateStatusRepository intermediateStatusRepository,
            IErrorMessageRepository errorMessageRepository)
        {
            this._logger = logger;
            this._intermediateStatusRepository = intermediateStatusRepository;

            var tlvInfos = new TlvInfo[]
            {
                new TlvInfo { Tag = "24", Description = "Display-texts", TryProcess = this.CleanupTextBuffer },
                new TlvInfo { Tag = "07", Description = "Text-Lines", TryProcess = this.AddTextLine },
            };

            this._tlvParser = new TlvParser(logger, tlvInfos);
            this._bmpParser = new BmpParser(logger, errorMessageRepository, this._tlvParser);

            this._tlvTextContent = new StringBuilder();
        }

        /// <inheritdoc />
        public string GetMessage(Span<byte> data)
        {
            if (data.Length <= 3)
            {
                this._logger.LogError($"{nameof(GetMessage)} - Invalid data length");
                return null;
            }

            var id = data[3];
            //var timeout = data[4];

            //Detect TLV Text
            if (id == 0xFF)
            {
                if (data.Length <= 6)
                {
                    this._logger.LogError($"{nameof(GetMessage)} - Invalid tlv data length");
                    return null;
                }

                var data1 = data.Slice(5);
                this._bmpParser.Parse(data1, null);
                return this._tlvTextContent.ToString();
            }

            var message = this._intermediateStatusRepository.GetMessage(id);
            if (string.IsNullOrEmpty(message))
            {
                this._logger.LogError($"{nameof(GetMessage)} - No message available for {id:X2}");
            }

            return message;
        }

        private bool CleanupTextBuffer(byte[] data, IResponse response)
        {
            this._tlvTextContent.Clear();

            return true;
        }

        private bool AddTextLine(byte[] data, IResponse response)
        {
            var textBlock = Encoding.GetEncoding(437).GetString(data);
            this._tlvTextContent.AppendLine(textBlock);

            return true;
        }
    }
}

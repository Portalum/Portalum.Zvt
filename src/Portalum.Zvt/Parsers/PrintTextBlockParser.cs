using Microsoft.Extensions.Logging;
using Portalum.Zvt.Models;
using Portalum.Zvt.Repositories;
using Portalum.Zvt.Responses;
using System;
using System.Text;

namespace Portalum.Zvt.Parsers
{
    /// <summary>
    /// PrintTextBlockParser
    /// </summary>
    public class PrintTextBlockParser : IPrintTextBlockParser
    {
        private readonly ILogger _logger;
        private readonly Encoding _encoding;
        private readonly BmpParser _bmpParser;
        private readonly TlvParser _tlvParser;

        private bool _completelyProcessed;
        private ReceiptType _receiptType;
        private readonly StringBuilder _receiptContent;

        /// <summary>
        /// PrintTextBlockParser
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="encoding"></param>
        /// <param name="errorMessageRepository"></param>
        public PrintTextBlockParser(
            ILogger logger,
            Encoding encoding,
            IErrorMessageRepository errorMessageRepository)
        {
            this._logger = logger;
            this._encoding = encoding;

            var tlvInfos = new TlvInfo[]
            {
                new TlvInfo { Tag = "1F07", Description = "ReceiptType", TryProcess = this.SetReceiptType },
                new TlvInfo { Tag = "25", Description = "Print-Texts", TryProcess = this.CleanupReceiptBuffer },
                new TlvInfo { Tag = "07", Description = "Text-Lines", TryProcess = this.AddTextLine },
                new TlvInfo { Tag = "09", Description = "EndOfReceipt", TryProcess = this.EndOfReceipt }
            };

            this._tlvParser = new TlvParser(logger, tlvInfos);
            this._bmpParser = new BmpParser(logger, encoding, errorMessageRepository, this._tlvParser);

            this._receiptType = ReceiptType.Unknown;
            this._receiptContent = new StringBuilder();
        }

        /// <inheritdoc />
        public ReceiptInfo Parse(Span<byte> data)
        {
            this._completelyProcessed = false;

            if (!this._bmpParser.Parse(data, null))
            {
                this._logger.LogError($"{nameof(Parse)} - Error on parsing data");
                return null;
            }

            return new ReceiptInfo
            {
                ReceiptType = this._receiptType,
                Content = this._receiptContent.ToString(),
                CompletelyProcessed = this._completelyProcessed
            };
        }

        private bool SetReceiptType(byte[] data, IResponse response)
        {
            if (data.Length == 0)
            {
                return true;
            }

            this._receiptType = (ReceiptType)data[0];

            return true;
        }

        private bool CleanupReceiptBuffer(byte[] data, IResponse response)
        {
            this._receiptContent.Clear();

            return true;
        }

        private bool AddTextLine(byte[] data, IResponse response)
        {
            var textBlock = this._encoding.GetString(data);
            this._receiptContent.AppendLine(textBlock);

            return true;
        }

        private bool EndOfReceipt(byte[] data, IResponse response)
        {
            this._completelyProcessed = true;

            return true;
        }
    }
}

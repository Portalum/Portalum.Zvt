using Microsoft.Extensions.Logging;
using Portalum.Zvt.Helpers;
using Portalum.Zvt.Models;
using Portalum.Zvt.Repositories;
using Portalum.Zvt.Responses;
using System;
using System.Text;

namespace Portalum.Zvt.Parsers
{
    /// <summary>
    /// StatusInformationParser
    /// </summary>
    public class StatusInformationParser : IStatusInformationParser
    {
        private readonly ILogger _logger;
        private readonly BmpParser _bmpParser;

        /// <summary>
        /// StatusInformationParser
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="encoding"></param>
        /// <param name="errorMessageRepository"></param>
        public StatusInformationParser(
            ILogger logger,
            Encoding encoding,
            IErrorMessageRepository errorMessageRepository)
        {
            this._logger = logger;

            //tag:24 (display-texts)
            //tag:07 (text-lines)
            //tag:60 (application)
            //tag:43 (application-ID)
            //tag:44 (application preferred name)
            //tag:46 (EMV-print-data (customer-receipt))
            //tag:47 (EMV-print-data (merchant-receipt))
            //tag:1F2B (Trace number (long format))

            var tlvInfos = new TlvInfo[]
            {
                new TlvInfo { Tag = "2F", Description = "Payment-type", TryProcess = null },
                new TlvInfo { Tag = "1F10", Description = "Cardholder authentication", TryProcess = this.SetCardholderAuthentication },
                new TlvInfo { Tag = "1F12", Description = "Card technology", TryProcess = this.SetCardTechnology },
                new TlvInfo { Tag = "60", Description = "Application", TryProcess = null },
                new TlvInfo { Tag = "1F2B", Description = "Trace number (long format)", TryProcess = this.SetTraceNumberLongFormat }
            };

            var tlvParser = new TlvParser(logger, tlvInfos);
            var bmpParser = new BmpParser(logger, encoding, errorMessageRepository, tlvParser);
            this._bmpParser = bmpParser;
        }

        /// <inheritdoc />
        public StatusInformation Parse(Span<byte> data)
        {
            var statusInformation = new StatusInformation();

            if (!this._bmpParser.Parse(data, statusInformation))
            {
                this._logger.LogError($"{nameof(Parse)} - Error on parsing data");
                return null;
            }

            return statusInformation;
        }

        private bool SetCardholderAuthentication(byte[] data, IResponse response)
        {
            if (response is IResponseCardholderAuthentication typedResponse)
            {
                var cardholderAuthentication = data[0];

                switch (cardholderAuthentication)
                {
                    case 0x00:
                        typedResponse.CardholderAuthentication = "No Cardholder authentication";
                        break;
                    case 0x01:
                        typedResponse.CardholderAuthentication = "Signature";
                        typedResponse.PrintoutNeeded = true;
                        break;
                    case 0x02:
                        typedResponse.CardholderAuthentication = "Online Pin";
                        break;
                    case 0x03:
                        typedResponse.CardholderAuthentication = "Offline encrypted Pin";
                        break;
                    case 0x04:
                        typedResponse.CardholderAuthentication = "Offline plaintext Pin";
                        break;
                    case 0x05:
                        typedResponse.CardholderAuthentication = "Offline encrypted Pin + signature";
                        typedResponse.PrintoutNeeded = true;
                        break;
                    case 0x06:
                        typedResponse.CardholderAuthentication = "Offline plaintext Pin + signature";
                        typedResponse.PrintoutNeeded = true;
                        break;
                    case 0x07:
                        typedResponse.CardholderAuthentication = "Online Pin + signature";
                        typedResponse.PrintoutNeeded = true;
                        break;
                    case 0xFF:
                        typedResponse.CardholderAuthentication = "Unknown cardholder verification";
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        private bool SetCardTechnology(byte[] data, IResponse response)
        {
            if (response is IResponseCardTechnology typedResponse)
            {
                var cardTechnology = data[0];

                switch (cardTechnology)
                {
                    case 0x00:
                        typedResponse.CardTechnology = "Magentic stripe";
                        break;
                    case 0x01:
                        typedResponse.CardTechnology = "Chip";
                        break;
                    case 0x02:
                        typedResponse.CardTechnology = "NFC";
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        private bool SetTraceNumberLongFormat(byte[] data, IResponse response)
        {
            if (response is IResponseTraceNumberLongFormat typedResponse)
            {
                var number = NumberHelper.BcdToInt(data);
                typedResponse.TraceNumberLongFormat = number;
            }

            return true;
        }
    }
}

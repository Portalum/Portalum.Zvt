using Microsoft.Extensions.Logging;
using Portalum.Payment.Zvt.Models;
using Portalum.Payment.Zvt.Repositories;
using Portalum.Payment.Zvt.Responses;
using System;

namespace Portalum.Payment.Zvt.Parsers
{
    /// <summary>
    /// StatusInformationParser
    /// </summary>
    public class StatusInformationParser
    {
        private readonly ILogger _logger;
        private readonly BmpParser _bmpParser;

        /// <summary>
        /// StatusInformationParser
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="errorMessageRepository"></param>
        public StatusInformationParser(
            ILogger logger,
            IErrorMessageRepository errorMessageRepository)
        {
            this._logger = logger;

            //tag:60 (application)
            //tag:43 (application-ID)
            //tag:44 (application preferred name)
            //tag:1F2B (Trace number (long format))

            var tlvInfos = new TlvInfo[]
            {
                new TlvInfo { Tag = "2F", Description = "Payment-type", TryProcess = null },
                new TlvInfo { Tag = "1F10", Description = "Cardholder authentication", TryProcess = this.SetCardholderAuthentication },
                new TlvInfo { Tag = "1F12", Description = "Card technology", TryProcess = this.SetCardTechnology }
            };

            var tlvParser = new TlvParser(logger, tlvInfos);
            var bmpParser = new BmpParser(logger, errorMessageRepository, tlvParser);
            this._bmpParser = bmpParser;
        }

        /// <summary>
        /// Parse
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
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
                        break;
                    case 0x06:
                        typedResponse.CardholderAuthentication = "Offline plaintext Pin + signature";
                        break;
                    case 0x07:
                        typedResponse.CardholderAuthentication = "Online Pin + signature";
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
    }
}

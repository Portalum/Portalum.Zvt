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

            //tag:41 (ZVT card-type-ID)
            //tag:4C (UID) - 10 bytes
            //tag:62 (list of applications on chip)
            //tag:1F0B (maximum pre-authorisation amount) - with leading zeros in Cent, BCD-packed, max. 6 byte
            //tag:1F14 (card identification item) - variable in length, binary
            //tag:1F45 (ATS)
            //tag:1F4C (Card [technology] type) - 1 byte - 0x00 ISO 7816-4, 0x01 MIFARE, 0x02 FeliCa
            //tag:1F4D (Card subtype) - 1 byte
            //tag:1F4F (??)
            //tag:1F50 (??)

            var tlvInfos = new TlvInfo[]
            {
                new TlvInfo { Tag = "2F", Description = "Payment-type", TryProcess = null },
                new TlvInfo { Tag = "1F10", Description = "Cardholder authentication", TryProcess = this.SetCardholderAuthentication },
                new TlvInfo { Tag = "1F12", Description = "Card technology", TryProcess = this.SetCardTechnology },
                new TlvInfo { Tag = "60", Description = "Application", TryProcess = null },
                new TlvInfo { Tag = "43", Description = "Application Id", TryProcess = this.SetApplicationId },
                new TlvInfo { Tag = "1F2B", Description = "Trace number (long format)", TryProcess = this.SetTraceNumberLongFormat },

                new TlvInfo { Tag = "41", Description = "ZVT card-type-ID", TryProcess = SetZvtCardTypeId },
                new TlvInfo { Tag = "4C", Description = "UID", TryProcess = SetCardUid },
                //new TlvInfo { Tag = "62", Description = "List of applications on chip", TryProcess = null },
                new TlvInfo { Tag = "1F0B", Description = "Maximum pre-authorisation amount", TryProcess = SetMaximumPreAuthorisationAmount },
                new TlvInfo { Tag = "1F14", Description = "Card identification item", TryProcess = SetCardIdentificationItem },
                new TlvInfo { Tag = "1F45", Description = "ATS", TryProcess = SetATS },
                new TlvInfo { Tag = "1F4C", Description = "Card technology type", TryProcess = SetCardTechnologyType },
                new TlvInfo { Tag = "1F4D", Description = "Card subtype", TryProcess = SetCardSubtype },
                //new TlvInfo { Tag = "1F4F", Description = "??", TryProcess = null },
                //new TlvInfo { Tag = "1F50", Description = "??", TryProcess = null }
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

        private bool SetApplicationId(byte[] data, IResponse response)
        {
            if (response is IResponseApplicationId typedResponse)
            {
                typedResponse.ApplicationId = ByteHelper.ByteArrayToHex(data).ToUpper();
                return true;
            }
            return false;
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
                        typedResponse.CardTechnology = "Magnetic stripe";
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

        private bool SetCardUid(byte[] data, IResponse response)
        {
            if (response is IResponseCardUid typedResponse)
            {
                typedResponse.CardUid = ByteHelper.ByteArrayToHex(data).ToUpper();
                return true;
            }
            return false;
        }

        private bool SetZvtCardTypeId(byte[] data, IResponse response)
        {
            if (response is IResponseZvtCardTypeId typedResponse)
            {
                typedResponse.ZvtCardTypeId = data[0];
                return true;
            }
            return false;
        }

        private bool SetCardTechnologyType(byte[] data, IResponse response)
        {
            if (response is IResponseCardTechnologyType typedResponse)
            {
                var cartType = data[0];

                switch (cartType)
                {
                    case 0x00:
                        typedResponse.CardTechnologyType = "ISO 7816-4";
                        break;
                    case 0x01:
                        typedResponse.CardTechnologyType = "MIFARE";
                        break;
                    case 0x02:
                        typedResponse.CardTechnologyType = "FeliCa";
                        break;
                    default:
                        return false;
                }
                return true;
            }
            return false;
        }

        private bool SetCardSubtype(byte[] data, IResponse response)
        {
            if (response is IResponseCardSubtype typedResponse)
            {
                typedResponse.CardSubtype = NumberHelper.BcdToInt(data);
                return true;
            }
            return false;
        }

        private bool SetMaximumPreAuthorisationAmount(byte[] data, IResponse response)
        {
            if (response is IResponseMaximumPreAuthorisationAmount typedResponse)
            {
                typedResponse.MaximumPreAuthorisationAmount = NumberHelper.BcdToInt(data);
                return true;
            }
            return false;
        }

        private bool SetATS(byte[] data, IResponse response)
        {
            if (response is IResponseATS typedResponse)
            {
                typedResponse.ATS = data;
                return true;
            }
            return false;
        }

        private bool SetCardIdentificationItem(byte[] data, IResponse response)
        {
            if (response is IResponseCardIdentificationItem typedResponse)
            {
                typedResponse.CardIdentificationItem = data;
                return true;
            }
            return false;
        }
    }
}

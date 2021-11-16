using Microsoft.Extensions.Logging;
using Portalum.Zvt.Helpers;
using Portalum.Zvt.Models;
using Portalum.Zvt.Repositories;
using Portalum.Zvt.Responses;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Portalum.Zvt.Parsers
{
    /// <summary>
    /// BMP Parser
    /// </summary>
    public class BmpParser
    {
        private readonly ILogger _logger;
        private readonly IErrorMessageRepository _errorMessageRepository;
        private readonly ITlvParser _tlvParser;

        private readonly Dictionary<byte, BmpInfo> _bmpInfos;
        private readonly Dictionary<int, string> _cardTypes;

        /// <summary>
        /// BMP Parser
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="errorMessageRepository"></param>
        /// <param name="tlvParser"></param>
        public BmpParser(
            ILogger logger,
            IErrorMessageRepository errorMessageRepository,
            ITlvParser tlvParser)
        {
            this._logger = logger;
            this._errorMessageRepository = errorMessageRepository;
            this._tlvParser = tlvParser;

            #region Card Types

            this._cardTypes = new Dictionary<int, string>
            {
                { 1, "DouglasCard" },
                //obsolete
                { 2, "ec-card (national, international, bank-customer card)" },
                { 3, "Miles&More" },
                //4 RFU
                { 5, "girocard" },
                { 6, "Mastercard" },
                { 7, "EAPS" },
                { 8, "American Express" },
                { 9, "Debit advice based on track 2 or EMV chip (e.g. EuroELV)" },
                { 10, "Visa" },
                { 11, "VISA electron" },
                { 12, "Diners" },
                { 13, "V PAY" },
                { 14, "JCB" },
                { 15, "REKA Card" },
                { 16, "Esso fleet-card" },
                { 17, "Happiness Cards" },
                { 18, "DKV/SVG" },
                { 19, "Transact Geschenkkarte" },
                { 20, "Shell fleet-card" },
                { 21, "Payeasy" },
                { 22, "DEA" },
                { 23, "boncard POINTS" },
                { 24, "Leaseplan" },
                { 25, "boncard PAY" },
                { 26, "OK" },
                { 27, "Klarmobil" },
                { 28, "UTA" },
                { 29, "Mobile World" },
                //Geldkarte (formerly also: ec-cash with Chip) 
                { 30, "Geldkarte" },
                //Maestro (formerly: edc)
                { 46, "Maestro" }
                //TODO: complete list -> List of ZVT-card-type IDs
            };

            #endregion

            #region BMP Infos

            var bmpInfos = new BmpInfo[]
            {
                new BmpInfo { Id = 0x01, DataLength = 1, Description = "Timeout", TryParse = null },
                new BmpInfo { Id = 0x02, DataLength = 1, Description = "Maximal number of status informations", TryParse = null },
                new BmpInfo { Id = 0x03, DataLength = 1, Description = "Service byte, bit-field. Meaning of bits depends on command this field is used in", TryParse = null },
                new BmpInfo { Id = 0x04, DataLength = 6, Description = "Amount in minor currency units", TryParse = ParseAmount },
                new BmpInfo { Id = 0x05, DataLength = 1, Description = "Pump number, range 00 - FF", TryParse = null },
                new BmpInfo { Id = 0x06, DataLength = 0, Description = "TLV-container; length according to TLV-encoding (not LLL-Var !)", TryParse = tlvParser.Parse },
                new BmpInfo { Id = 0x0B, DataLength = 3, Description = "Trace number", TryParse = this.ParseTraceNumber },
                new BmpInfo { Id = 0x0C, DataLength = 3, Description = "Time, format HHMMSS", TryParse = this.ParseTime },
                new BmpInfo { Id = 0x0D, DataLength = 2, Description = "Date, format MMDD (see also AA)", TryParse = null },
                new BmpInfo { Id = 0x0E, DataLength = 2, Description = "Expiry-date, format YYMM", TryParse = this.ParseExpiryDate },
                new BmpInfo { Id = 0x17, DataLength = 2, Description = "Card sequence-number", TryParse = this.ParseCardSequenceNumber },
                new BmpInfo { Id = 0x19, DataLength = 1, Description = "Status-byte as defined in Registration (06 00) / Payment-type as defined in Authorization (06 01) / Card-type as defined in Read Card (06 C0)", TryParse = null },
                new BmpInfo { Id = 0x22, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "PAN / EF_ID, 'E' used to indicate a masked numeric digit1. If the card-number contains an odd number of digits, it is padded with an ‘F’.", TryParse = null },
                new BmpInfo { Id = 0x23, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Track 2 data, without start and end markers; 'E' used to indicate a masked numeric digit", TryParse = null },
                new BmpInfo { Id = 0x24, DataLength = 3, CalculateDataLength = this.GetDataLengthLLL, Description = "Track 3 data, without start and end markers; 'E' used to indicate a masked numeric digit", TryParse = null },
                new BmpInfo { Id = 0x27, DataLength = 1, Description = "Result-Code as defined in chapter Error-Messages", TryParse = this.ParseErrorCode },
                new BmpInfo { Id = 0x29, DataLength = 4, Description = "Terminal identifier", TryParse = this.ParseTerminalIdentifier },
                new BmpInfo { Id = 0x2A, DataLength = 15, Description = "VU-number", TryParse = this.ParseVuNumber },
                new BmpInfo { Id = 0x2D, DataLength = 2, Description = "Track 1 data, without start and end markers", TryParse = null },
                new BmpInfo { Id = 0x2E, DataLength = 3, CalculateDataLength = this.GetDataLengthLL, Description = "Synchronous chip data", TryParse = null },
                new BmpInfo { Id = 0x37, DataLength = 3, CalculateDataLength = this.GetDataLengthLLL, Description = "Trace-number of the original transaction for reversal", TryParse = null },
                new BmpInfo { Id = 0x3A, DataLength = 2, Description = "CVV/CVC value, right padded with ‘F’ if less than 4 digits", TryParse = null },
                new BmpInfo { Id = 0x3B, DataLength = 8, Description = "AID authorisation-attribute", TryParse = this.ParseAuthorisationAttribute },
                new BmpInfo { Id = 0x3C, DataLength = 3, CalculateDataLength = this.GetDataLengthLLL, Description = "Additional-data/additional-text", TryParse = this.ParseAdditionalText },
                new BmpInfo { Id = 0x3D, DataLength = 3, Description = "Password", TryParse = null },
                new BmpInfo { Id = 0x49, DataLength = 2, Description = "Currency code", TryParse = this.ParseCurrencyCode },
                new BmpInfo { Id = 0x60, DataLength = 3, CalculateDataLength = this.GetDataLengthLLL, Description = "Individual totals", TryParse = null },
                new BmpInfo { Id = 0x70, DataLength = 4, Description = "Uniquely identifies Display Image request. In case image data is transmitted by more than one Display Image message (image data is chunked) then each of them has to have the same request-id set.", TryParse = null },
                new BmpInfo { Id = 0x71, DataLength = 4, Description = "Total size of the image that will be displayed. Image-size is 4 bytes long. This field is used when image data is chunked and pays control role to ensure receiver that sum of all received image data chunks is correct.", TryParse = null },
                new BmpInfo { Id = 0x72, DataLength = 1, Description = "MIME type of the image.", TryParse = null },
                new BmpInfo { Id = 0x73, DataLength = 1, Description = "Image encoding type.", TryParse = null },
                new BmpInfo { Id = 0x74, DataLength = 1, Description = "Total number of chunks of the image to display.", TryParse = null },
                new BmpInfo { Id = 0x75, DataLength = 1, Description = "Index of the chunk of the image data.", TryParse = null },
                new BmpInfo { Id = 0x87, DataLength = 2, Description = "Receipt-number", TryParse = this.ParseReceiptNumber },
                new BmpInfo { Id = 0x88, DataLength = 3, Description = "Turnover record number", TryParse = this.ParseTurnoverRecordNumber },
                new BmpInfo { Id = 0x8A, DataLength = 1, Description = "Card-type (card-number according to ZVT-protocol; see also 8C)", TryParse = this.ParseCardType },
                new BmpInfo { Id = 0x8B, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Card-name", TryParse = this.ParseCardName },
                new BmpInfo { Id = 0x8C, DataLength = 1, Description = "Card-type-ID of the network operator (see also 8A)", TryParse = null },
                new BmpInfo { Id = 0x9A, DataLength = 3, CalculateDataLength = this.GetDataLengthLLL, Description = "GeldKarte payments-/ failed-payment record/total record Geldkarte", TryParse = null },
                new BmpInfo { Id = 0xA0, DataLength = 1, Description = "Result-code-AS", TryParse = null },
                new BmpInfo { Id = 0xA7, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Chip-data, EF_ID", TryParse = null },
                new BmpInfo { Id = 0xAA, DataLength = 3, Description = "Date, format YYMMDD (see also 0D)", TryParse = null },
                new BmpInfo { Id = 0xAF, DataLength = 3, CalculateDataLength = this.GetDataLengthLLL, Description = "EF_Info", TryParse = null },
                new BmpInfo { Id = 0xBA, DataLength = 5, Description = "AID-parameter", TryParse = null },
                new BmpInfo { Id = 0xD0, DataLength = 1, Description = "Algorithm key", TryParse = null },
                new BmpInfo { Id = 0xD1, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Card offset/PIN-data", TryParse = null },
                new BmpInfo { Id = 0xD2, DataLength = 1, Description = "Card output direction. Determines the direction of card output for a motor-reader, default = ‘00’", TryParse = null },
                new BmpInfo { Id = 0xD3, DataLength = 1, Description = "DUKPT key identifier", TryParse = null },
                new BmpInfo { Id = 0xE0, DataLength = 1, Description = "Minimal length of the input", TryParse = null },
                new BmpInfo { Id = 0xE1, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text2 line 1", TryParse = null },
                new BmpInfo { Id = 0xE2, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text2 line 2", TryParse = null },
                new BmpInfo { Id = 0xE3, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text2 line 3", TryParse = null },
                new BmpInfo { Id = 0xE4, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text2 line 4", TryParse = null },
                new BmpInfo { Id = 0xE5, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text2 line 5", TryParse = null },
                new BmpInfo { Id = 0xE6, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text2 line 6", TryParse = null },
                new BmpInfo { Id = 0xE7, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text2 line 7", TryParse = null },
                new BmpInfo { Id = 0xE8, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text2 line 8", TryParse = null },
                new BmpInfo { Id = 0xE9, DataLength = 1, Description = "Maximal length of the input", TryParse = null },
                new BmpInfo { Id = 0xEA, DataLength = 1, Description = "Echo the input", TryParse = null },
                new BmpInfo { Id = 0xEB, DataLength = 8, Description = "MAC over text 1 and text 2", TryParse = null },
                new BmpInfo { Id = 0xF0, DataLength = 1, Description = "Display-duration in seconds. ‘00’ means infinite. Default-value = ‘00’.", TryParse = null },
                new BmpInfo { Id = 0xF1, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text1 line 1", TryParse = null },
                new BmpInfo { Id = 0xF2, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text1 line 2", TryParse = null },
                new BmpInfo { Id = 0xF3, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text1 line 3", TryParse = null },
                new BmpInfo { Id = 0xF4, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text1 line 4", TryParse = null },
                new BmpInfo { Id = 0xF5, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text1 line 5", TryParse = null },
                new BmpInfo { Id = 0xF6, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text1 line 6", TryParse = null },
                new BmpInfo { Id = 0xF7, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text1 line 7", TryParse = null },
                new BmpInfo { Id = 0xF8, DataLength = 2, CalculateDataLength = this.GetDataLengthLL, Description = "Text1 line 8", TryParse = null },
                new BmpInfo { Id = 0xF9, DataLength = 1, Description = "Number of beep-tones, default-value = ‘00’", TryParse = null },
                new BmpInfo { Id = 0xFA, DataLength = 1, Description = "Card reader activation. Defines whether the card-reader should be activated or deactivated. Only an activated card-reader will draw-in the card or release the shutter.", TryParse = null },
                new BmpInfo { Id = 0xFB, DataLength = 1, Description = "Confirmation the input with <OK> required", TryParse = null },
                new BmpInfo { Id = 0xFC, DataLength = 1, Description = "Dialog-control", TryParse = null },
                new BmpInfo { Id = 0xFD, DataLength = 1, Description = "Display device on which text should be shown. The default display-device type is the terminal display.", TryParse = null },
            };

            this._bmpInfos = new Dictionary<byte, BmpInfo>();
            foreach (var bmpInfo in bmpInfos)
            {
                if (!this._bmpInfos.TryAdd(bmpInfo.Id, bmpInfo))
                {
                    throw new NotSupportedException($"Cannot add bmpInfo {bmpInfo.Id:X2} (duplicate key)");
                }
            }

            #endregion
        }

        private BmpInfo GetBmpInfo(byte command)
        {
            if (!this._bmpInfos.TryGetValue(command, out var bmpInfo))
            {
                this._logger.LogError($"{nameof(GetBmpInfo)} - No processing logic available for {command:X2}");
                return null;
            }

            return bmpInfo;
        }

        public bool Parse(Span<byte> data, IResponse response)
        {
            var currentPosition = 0;

            while (currentPosition < data.Length)
            {
                var command = data[currentPosition];
                var bmpInfo = this.GetBmpInfo(command);
                if (bmpInfo == null)
                {
                    this._logger.LogError($"{nameof(Parse)} - No processing logic available for {command:X2}");
                    return false;
                }
                currentPosition++;

                var dataLength = bmpInfo.DataLength;

                //Variable data length
                if (bmpInfo.CalculateDataLength != null)
                {
                    var dataLengthData = data.Slice(currentPosition, dataLength).ToArray();
                    currentPosition += dataLength;
                    dataLength = bmpInfo.CalculateDataLength.Invoke(dataLengthData);
                }

                byte[] bmpData;
                if (dataLength == 0)
                {
                    //Detect TLV Length
                    var tlvLengthInfo = this._tlvParser.GetLength(data.Slice(currentPosition));
                    dataLength = tlvLengthInfo.NumberOfBytesThatCanBeSkipped + tlvLengthInfo.Length;
                }

                bmpData = data.Slice(currentPosition, dataLength).ToArray();

                if (bmpInfo.TryParse != null)
                {
                    if (!bmpInfo.TryParse(bmpData, response))
                    {
                        this._logger.LogWarning($"{nameof(Parse)} - Cannot parse data for {bmpInfo.Id:X2}");
                    }
                }
                else
                {
                    this._logger.LogInformation($"{nameof(Parse)} - No parser available for {bmpInfo.Id:X2} {bmpInfo.Description}");
                }

                currentPosition += dataLength;
            }

            return true;
        }

        /// <summary>
        /// Parse LLVAR Length
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int GetDataLengthLL(byte[] data)
        {
            if (data.Length != 2)
            {
                return 0;
            }

            var firstByteHex = data[0].ToString("X2");
            var secondByteHex = data[1].ToString("X2");

            if (!int.TryParse(firstByteHex[1].ToString(), out var n1))
            {
                return 0;
            }

            if (!int.TryParse(secondByteHex[1].ToString(), out var n2))
            {
                return 0;
            }

            var result = n1 * 10;
            result += n2;
            return result;
        }

        /// <summary>
        /// Parse LLLVAR Length
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int GetDataLengthLLL(byte[] data)
        {
            if (data.Length != 3)
            {
                return 0;
            }

            var firstByteHex = data[0].ToString("X2");
            var secondByteHex = data[1].ToString("X2");
            var thirdByteHex = data[2].ToString("X2");

            if (!int.TryParse(firstByteHex[1].ToString(), out var n1))
            {
                return 0;
            }

            if (!int.TryParse(secondByteHex[1].ToString(), out var n2))
            {
                return 0;
            }

            if (!int.TryParse(thirdByteHex[1].ToString(), out var n3))
            {
                return 0;
            }

            var result = n1 * 100;
            result += n2 * 10;
            result += n3;
            return result;
        }

        private bool ParseErrorCode(byte[] data, IResponse response)
        {
            var errorMessage = this._errorMessageRepository.GetMessage(data[0]);

            if (response is IResponseErrorMessage typedResponse)
            {
                typedResponse.ErrorMessage = errorMessage;
                return true;
            }

            return false;
        }

        private bool ParseTime(byte[] data, IResponse response)
        {
            if (response is IResponseTime typedResponse)
            {
                if (data.Length != 3)
                {
                    return false;
                }

                var timeInHex = ByteHelper.ByteArrayToHex(data);
                if (!TimeSpan.TryParseExact(timeInHex, "hhmmss", CultureInfo.InvariantCulture, out var time))
                {
                    return false;
                }

                typedResponse.Time = time;
                return true;
            }

            return false;
        }

        private bool ParseExpiryDate(byte[] data, IResponse response)
        {
            if (response is IResponseExpiryDate typedResponse)
            {
                if (data.Length != 2)
                {
                    return false;
                }

                var expiryDate = ByteHelper.ByteArrayToHex(data);

                if (!int.TryParse(expiryDate.Substring(0,2), out var year))
                {
                    return false;
                }

                if (!int.TryParse(expiryDate.Substring(2, 2), out var month))
                {
                    return false;
                }

                typedResponse.ExpiryDateMonth = month;
                typedResponse.ExpiryDateYear = year;

                return true;
            }

            return false;
        }

        private bool ParseCardSequenceNumber(byte[] data, IResponse response)
        {
            if (response is IResponseCardSequenceNumber typedResponse)
            {
                if (data.Length != 2)
                {
                    return false;
                }

                var number = ByteHelper.ByteArrayToHex(data);

                if (!int.TryParse(number, out var cardSequenceNumber))
                {
                    return false;
                }

                typedResponse.CardSequenceNumber = cardSequenceNumber;

                return true;
            }

            return false;
        }

        private bool ParseCardType(byte[] data, IResponse response)
        {
            if (response is IResponseCardType typedResponse)
            {
                if (data.Length != 1)
                {
                    return false;
                }

                if (!this._cardTypes.TryGetValue(data[0], out var cardType))
                {
                    return false;
                }

                typedResponse.CardType = cardType;

                return true;
            }

            return false;
        }

        private bool ParseTurnoverRecordNumber(byte[] data, IResponse response)
        {
            if (response is IResponseTurnoverRecordNumber typedResponse)
            {
                if (data.Length != 3)
                {
                    return false;
                }

                var number = ByteHelper.ByteArrayToHex(data);
                if (!int.TryParse(number, out var turnoverRecordNumber))
                {
                    return false;
                }

                typedResponse.TurnoverRecordNumber = turnoverRecordNumber;

                return true;
            }

            return false;
        }

        private bool ParseCurrencyCode(byte[] data, IResponse response)
        {
            if (response is IResponseCurrencyCode typedResponse)
            {
                var currencyCode = NumberHelper.BcdToInt(data);
                typedResponse.CurrencyCode = currencyCode;

                return true;
            }

            return false;
        }

        private bool ParseTraceNumber(byte[] data, IResponse response)
        {
            if (response is IResponseTraceNumber typedResponse)
            {
                var number = NumberHelper.BcdToInt(data);
                typedResponse.TraceNumber = number;

                return true;
            }

            return false;
        }
        
        private bool ParseVuNumber(byte[] data, IResponse response)
        {
            if (response is IResponseVuNumber typedResponse)
            {
                var number = Encoding.ASCII.GetString(data).TrimEnd();
                typedResponse.VuNumber = number;

                return true;
            }

            return false;
        }

        private bool ParseAuthorisationAttribute(byte[] data, IResponse response)
        {
            if (response is IResponseAidAuthorisationAttribute typedResponse)
            {
                var number = Encoding.ASCII.GetString(data).TrimEnd('\0');
                typedResponse.AidAuthorisationAttribute = number;

                return true;
            }

            return false;
        }

        private bool ParseReceiptNumber(byte[] data, IResponse response)
        {
            if (response is IResponseReceiptNumber typedResponse)
            {
                var number = NumberHelper.BcdToInt(data);
                typedResponse.ReceiptNumber = number;

                return true;
            }

            return false;
        }

        private bool ParseAdditionalText(byte[] data, IResponse response)
        {
            if (response is IResponseAdditionalText typedResponse)
            {
                var text = Encoding.UTF7.GetString(data);
                typedResponse.AdditionalText = text;

                return true;
            }

            return false;
        }

        private bool ParseTerminalIdentifier(byte[] data, IResponse response)
        {
            if (response is IResponseTerminalIdentifier typedResponse)
            {
                Array.Reverse(data);
                var terminalIdentifier = BitConverter.ToInt32(data, 0);

                typedResponse.TerminalIdentifier = terminalIdentifier;

                return true;
            }

            return false;
        }

        private bool ParseCardName(byte[] data, IResponse response)
        {
            if (response is IResponseCardName typedResponse)
            {
                var cardName = Encoding.UTF7.GetString(data);
                typedResponse.CardName = cardName.TrimEnd('\0');

                return true;
            }

            return false;
        }

        private bool ParseAmount(byte[] data, IResponse response)
        {
            if (response is IResponseAmount typedResponse)
            {
                var number = NumberHelper.BcdToInt(data);
                var amount = number / 100.0M;
                typedResponse.Amount = amount;

                return true;
            }

            return false;
        }
    }
}

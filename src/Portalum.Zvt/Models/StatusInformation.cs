using Portalum.Zvt.Responses;
using System;

namespace Portalum.Zvt.Models
{
    public class StatusInformation :
        IResponse,
        IResponseErrorMessage,
        IResponseErrorCode,
        IResponseAdditionalText,
        IResponseTerminalIdentifier,
        IResponseAmount,
        IResponseCardName,
        IResponseCardholderAuthentication,
        IResponseCardTechnology,
        IResponseTime,
        IResponseCurrencyCode,
        IResponseReceiptNumber,
        IResponseTraceNumber,
        IResponseTraceNumberLongFormat,
        IResponseVuNumber,
        IResponseAidAuthorisationAttribute,
        IResponseExpiryDate,
        IResponseCardSequenceNumber,
        IResponseTurnoverRecordNumber,
        IResponseCardType
    {
        public string ErrorMessage { get; set; }
        public int TerminalIdentifier { get; set; }
        public string AdditionalText { get; set; }
        public string CardName { get; set; }
        public decimal Amount { get; set; }
        public string CardholderAuthentication { get; set; }
        public bool PrintoutNeeded { get; set; }
        public string CardTechnology { get; set; }
        public TimeSpan Time { get; set; }
        public int CurrencyCode { get; set; }
        public int ReceiptNumber { get; set; }
        public int TraceNumber { get; set; }
        public int TraceNumberLongFormat { get; set; }
        public string VuNumber { get; set; }
        public string AidAuthorisationAttribute { get; set; }
        public int ExpiryDateYear { get; set; }
        public int ExpiryDateMonth { get; set; }
        public int CardSequenceNumber { get; set; }
        public int TurnoverRecordNumber { get; set; }
        public string CardType { get; set; }
        public byte ErrorCode { get; set; }
    }
}
using Portalum.Payment.Zvt.Responses;
using System;

namespace Portalum.Payment.Zvt.Models
{
    public class StatusInformation :
        IResponse,
        IResponseErrorMessage,
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
        IResponseVuNumber,
        IResponseAidAuthorisationAttribute
    {
        public string ErrorMessage { get; set; }
        public int TerminalIdentifier { get; set; }
        public string AdditionalText { get; set; }
        public string CardName { get; set; }
        public decimal Amount { get; set; }
        public string CardholderAuthentication { get; set; }
        public string CardTechnology { get; set; }
        public TimeSpan Time { get; set; }
        public int CurrencyCode { get; set; }
        public int ReceiptNumber { get; set; }
        public int TraceNumber { get; set; }
        public string VuNumber { get; set; }
        public string AidAuthorisationAttribute { get; set; }
    }
}

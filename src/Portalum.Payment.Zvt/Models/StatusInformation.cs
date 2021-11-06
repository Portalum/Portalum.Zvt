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
        IResponseCurrencyCode
    {
        public string ErrorMessage { get; set; }
        public string TraceNumber { get; set; }
        public int TerminalIdentifier { get; set; }
        public string AdditionalText { get; set; }
        public string CardName { get; set; }
        public decimal Amount { get; set; }
        public string CardholderAuthentication { get; set; }
        public string CardTechnology { get; set; }
        public TimeSpan Time { get; set; }
        public int CurrencyCode { get; set; }
    }
}

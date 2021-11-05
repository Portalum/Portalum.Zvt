using System.Collections.Generic;

namespace Portalum.Payment.Zvt.Repositories
{
    /// <summary>
    /// English ErrorMessageRepository
    /// </summary>
    public class EnglishErrorMessageRepository : IErrorMessageRepository
    {
        private readonly Dictionary<byte, string> _errorCodes;

        /// <summary>
        /// English ErrorMessageRepository
        /// </summary>
        public EnglishErrorMessageRepository()
        {
            this._errorCodes = new Dictionary<byte, string>
            {
                { 0x00, "no error" },
                { 0x64, "card not readable (LRC-/parity-error)" },
                { 0x65, "card-data not present (neither track-data nor chip found)" },
                { 0x66, "processing-error (also for problems with card-reader mechanism)" },
                { 0x67, "function not permitted for ec- and Maestro-cards" },
                { 0x68, "function not permitted for credit- and tank-cards" },
                { 0x6A, "turnover-file full" },
                { 0x6B, "function deactivated (PT not registered)" },
                { 0x6C, "abort via timeout or abort-key" },
                { 0x6E, "card in blocked-list (response to command 06 E4)" },
                { 0x6F, "wrong currency" },
                { 0x71, "credit not sufficient (chip-card)" },
                { 0x72, "chip error" },
                { 0x73, "card-data incorrect (e.g. country-key check, checksum-error)" },
                { 0x74, "DUKPT engine exhausted" },
                { 0x75, "text not authentic" },
                { 0x76, "PAN not in white list" },
                { 0x77, "end-of-day batch not possible" },
                { 0x78, "card expired" },
                { 0x79, "card not yet valid" },
                { 0x7A, "card unknown" },
                { 0x7B, "fallback to magnetic stripe for girocard not possible" },
                { 0x7C, "fallback to magnetic stripe not possible (used for non girocard cards)" },
                { 0x7D, "communication error (communication module does not answer or is not present)" },
                { 0x7E, "fallback to magnetic stripe not possible, debit advice possible (used only for girocard)" },
                { 0x83, "function not possible" },
                { 0x85, "key missing" },
                { 0x89, "PIN-pad defective" },
                { 0x9A, "ZVT protocol error. e. g. parsing error, mandatory message element missing" },
                { 0x9B, "error from dial-up/communication fault" },
                { 0x9C, "please wait" },
                { 0xA0, "receiver not ready" },
                { 0xA1, "remote station does not respond" },
                { 0xA3, "no connection" },
                { 0xA4, "submission of Geldkarte not possible" },
                { 0xA5, "function not allowed due to PCI-DSS/P2PE rules" },
                { 0xB1, "memory full" },
                { 0xB2, "merchant-journal full" },
                { 0xB4, "already reversed" },
                { 0xB5, "reversal not possible" },
                { 0xB7, "pre-authorisation incorrect (amount too high) or amount wrong" },
                { 0xB8, "error pre-authorisation" },
                { 0xBF, "voltage supply to low (external power supply)" },
                { 0xC0, "card locking mechanism defective" },
                { 0xC1, "merchant-card locked" },
                { 0xC2, "diagnosis required" },
                { 0xC3, "maximum amount exceeded" },
                { 0xC4, "card-profile invalid. New card-profiles must be loaded." },
                { 0xC5, "payment method not supported" },
                { 0xC6, "currency not applicable" },
                { 0xC8, "amount too small" },
                { 0xC9, "max. transaction-amount too small" },
                { 0xCB, "function only allowed in EURO" },
                { 0xCC, "printer not ready" },
                { 0xCD, "Cashback not possible" },
                { 0xD2, "function not permitted for service-cards/bank-customer-cards" },
                { 0xDC, "card inserted" },
                { 0xDD, "error during card-eject (for motor-insertion reader)" },
                { 0xDE, "error during card-insertion (for motor-insertion reader)" },
                { 0xE0, "remote-maintenance activated" },
                { 0xE2, "card-reader does not answer / card-reader defective" },
                { 0xE3, "shutter closed" },
                { 0xE4, "Terminal activation required" },
                { 0xE7, "min. one goods-group not found" },
                { 0xE8, "no goods-groups-table loaded" },
                { 0xE9, "restriction-code not permitted" },
                { 0xEA, "card-code not permitted (e.g. card not activated via Diagnosis)" },
                { 0xEB, "function not executable (PIN-algorithm unknown)" },
                { 0xEC, "PIN-processing not possible" },
                { 0xED, "PIN-pad defective" },
                { 0xF0, "open end-of-day batch present" },
                { 0xF1, "ec-cash/Maestro offline error" },
                { 0xF5, "OPT-error" },
                { 0xF6, "OPT-data not available (= OPT personalisation required)" },
                { 0xFA, "error transmitting offline-transactions (clearing error)" },
                { 0xFB, "turnover data-set defective" },
                { 0xFC, "necessary device not present or defective" },
                { 0xFD, "baudrate not supported" },
                { 0xFE, "register unknown" },
                { 0xFF, "system error (= other/unknown error), See TLV tags 1F16 and 1F17" }
            };
        }

        /// <inheritdoc />
        public string GetMessage(byte errorCode)
        {
            if (this._errorCodes.TryGetValue(errorCode, out var errorMessage))
            {
                return errorMessage;
            }

            return null;
        }
    }
}

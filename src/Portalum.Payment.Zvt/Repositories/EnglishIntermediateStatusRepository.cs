using System.Collections.Generic;

namespace Portalum.Payment.Zvt.Repositories
{
    /// <summary>
    /// English IntermediateStatusRepository
    /// </summary>
    public class EnglishIntermediateStatusRepository : IIntermediateStatusRepository
    {
        private readonly Dictionary<byte, string> _statusCodes;

        /// <summary>
        /// English IntermediateStatusRepository
        /// </summary>
        public EnglishIntermediateStatusRepository()
        {
            this._statusCodes = new Dictionary<byte, string>
            {
                { 0x00, "PT is waiting for amount-confirmation" },
                { 0x01, "Please watch PIN-Pad" },
                { 0x02, "Please watch PIN-Pad" },
                { 0x03, "Not accepted" },
                { 0x04, "PT is waiting for response from FEP" },
                { 0x05, "PT is sending auto-reversal" },
                { 0x06, "PT is sending post-bookings" },
                { 0x07, "Card not admitted" },
                { 0x08, "Card unknown / undefined" },
                { 0x09, "Expired card" },
                { 0x0A, "Insert card" },
                { 0x0B, "Please remove card!" },
                { 0x0C, "Card not readable" },
                { 0x0D, "Processing error" },
                { 0x0E, "Please wait..." },
                { 0x0F, "PT is commencing an automatic end-ofday batch" },
                { 0x10, "Invalid card" },
                { 0x11, "Balance display" },
                { 0x12, "System malfunction" },
                { 0x13, "Payment not possible" },
                { 0x14, "Credit not sufficient" },
                { 0x15, "Incorrect PIN" },
                { 0x16, "Limit not sufficient" },
                { 0x17, "Please wait..." },
                { 0x18, "PIN try limit exceeded" },
                { 0x19, "Card-data incorrect" },
                { 0x1A, "Service-mode" },
                { 0x1B, "Approved. Please fill-up" },
                { 0x1C, "Approved. Please take goods" },
                { 0x1D, "Declined" },
                { 0x26, "PT is waiting for input of the mobilenumber" },
                { 0x27, "PT is waiting for repeat of mobile number" },
                { 0x28, "Currency selection, please wait..." },
                { 0x29, "Language selection, please wait..." },
                { 0x2A, "For loading please insert card" },
                { 0x2B, "Emergency transaction, please wait" },
                { 0x2C, "Application selection, please wait" },
                { 0x41, "Please watch PIN-Pad\nPlease remove card!" },
                { 0x42, "Please watch PIN-Pad\nPlease remove card!" },
                { 0x43, "Not accepted\nPlease remove card!" },
                { 0x44, "PT is waiting for response from FEP\nPlease remove card!" },
                { 0x45, "PT is sending auto-reversal\nPlease remove card!" },
                { 0x46, "PT is sending post-booking\nPlease remove card!" },
                { 0x47, "Card not admitted\nPlease remove card!" },
                { 0x48, "Card unknown / undefined\nPlease remove card!" },
                { 0x49, "Expired card\nPlease remove card!" },
                { 0x4A, "" }, //No text in the documentation
                { 0x4B, "Please remove card!" },
                { 0x4C, "Card not readable\nPlease remove card!" },
                { 0x4D, "Processing error\nPlease remove card!" },
                { 0x4E, "Please wait\nPlease remove card!" },
                { 0x4F, "PT is commencing an automatic end-ofday batch\nPlease remove card!" },
                { 0x50, "Invalid card\nPlease remove card!" },
                { 0x51, "Balance display\nPlease remove card!" },
                { 0x52, "System malfunction\nPlease remove card!" },
                { 0x53, "Payment not possible\nPlease remove card!" },
                { 0x54, "Credit not sufficient\nPlease remove card!" },
                { 0x55, "Incorrect PIN\nPlease remove card!" },
                { 0x56, "Limit not sufficient\nPlease remove card!" },
                { 0x57, "Please wait...\nPlease remove card!" },
                { 0x58, "PIN try limit exceeded\nPlease remove card!" },
                { 0x59, "Card-data incorrect\nPlease remove card!" },
                { 0x5A, "Service-mode\nPlease remove card!" },
                { 0x5B, "Approved. Please fill-up\nPlease remove card!" },
                { 0x5C, "Approved. Please take goods\nPlease remove card!" },
                { 0x5D, "Declined\nPlease remove card!" },
                { 0x66, "PT is waiting for input of the mobil-number\nPlease remove card!" },
                { 0x67, "PT is waiting for repeat of the mobilnumber\nPlease remove card!" },
                { 0x68, "PT has detected customer card insertion" },
                { 0x69, "Please select DCC" },
                { 0xC7, "PT is waiting for input of the mileage" },
                { 0xC8, "PT is waiting for cashier" },
                { 0xC9, "PT is commencing an automatic diagnosis" },
                { 0xCA, "PT is commencing an automatic initialisation" },
                { 0xCB, "Merchant-journal full" },
                { 0xCC, "Debit advice not possible, PIN required" },
                { 0xD2, "Connecting dial-up" },
                { 0xD3, "Dial-up connection made" },
                { 0xE0, "PT is waiting for application-selection" },
                { 0xE1, "PT is waiting for language-selection" },
                { 0xE2, "PT requests to use the cleaning card" },
                { 0xF1, "Offline" },
                { 0xF2, "Online" },
                { 0xF3, "Offline transaction" },
                { 0xFF, "no appropriate ZVT status code matches the status. See TLV tags 24 and 07" }
            };
        }

        /// <inheritdoc />
        public string GetMessage(byte errorCode)
        {
            if (this._statusCodes.TryGetValue(errorCode, out var errorMessage))
            {
                return errorMessage;
            }

            return null;
        }
    }
}

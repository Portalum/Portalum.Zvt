namespace Portalum.Payment.Zvt
{
    public class RegistrationConfig
    {
        /// <summary>
        /// Electronic Cash Register prints the receipts for payment functions
        /// </summary>
        public bool ReceiptPrintoutForPaymentFunctionsViaPaymentTerminal = false;

        /// <summary>
        /// Electronic Cash Register prints the receipts for administration functions
        /// </summary>
        public bool ReceiptPrintoutForAdministrationFunctionsViaPaymentTerminal = false;

        /// <summary>
        /// Electronic Cash Register requires intermediate status-Information. The PT sends no intermediate status-information if not logged-on.
        /// </summary>
        public bool SendIntermediateStatusInformation = false;

        //public bool ReceiptPrintType 

        /// <summary>
        /// Electronic Cash Register controls payment function
        /// </summary>
        public bool AllowStartPaymentViaPaymentTerminal = false;

        /// <summary>
        /// Electronic Cash Register controls administration function
        /// </summary>
        public bool AllowAdministrationViaPaymentTerminal = false;
    }
}

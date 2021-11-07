using Portalum.Payment.Zvt.Helpers;

namespace Portalum.Payment.Zvt
{
    /// <summary>
    /// Payment Terminal Registration Config
    /// </summary>
    public class RegistrationConfig
    {
        #region ConfigByte

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

        #endregion

        #region ServiceByte

        /// <summary>
        /// The service menu of the payment terminal must not be assigned to the function key
        /// </summary>
        public bool ServiceMenuIsDisabledOnTheFunctionKeyOfThePaymentTerminal = true;

        /// <summary>
        /// The display texts for the Commands Authorisation, Pre-initialisation and Reversal will be displayed in capitals
        /// </summary>
        public bool TextAtDisplayInCapitalLettersAtThePaymentTerminal = false;

        /// <summary>
        /// Activate TLV Support
        /// </summary>
        public bool ActivateTlvSupport = true;

        #endregion

        /// <summary>
        /// Get config byte
        /// </summary>
        /// <returns></returns>
        public byte GetConfigByte()
        {
            var configByte = new byte();
            if (!this.ReceiptPrintoutForPaymentFunctionsViaPaymentTerminal)
            {
                configByte = BitHelper.SetBit(configByte, 1);
            }
            if (!this.ReceiptPrintoutForAdministrationFunctionsViaPaymentTerminal)
            {
                configByte = BitHelper.SetBit(configByte, 2);
            }
            if (this.SendIntermediateStatusInformation)
            {
                configByte = BitHelper.SetBit(configByte, 3);
            }
            if (!this.AllowStartPaymentViaPaymentTerminal)
            {
                configByte = BitHelper.SetBit(configByte, 4);
            }
            if (!this.AllowAdministrationViaPaymentTerminal)
            {
                configByte = BitHelper.SetBit(configByte, 5);
            }

            configByte = BitHelper.SetBit(configByte, 7); //ECR print-type

            return configByte;
        }

        /// <summary>
        /// Get service byte
        /// </summary>
        /// <returns></returns>
        public byte GetServiceByte()
        {
            var serviceByte = new byte();
            if (this.ServiceMenuIsDisabledOnTheFunctionKeyOfThePaymentTerminal)
            {
                serviceByte = BitHelper.SetBit(serviceByte, 0);
            }
            if (this.TextAtDisplayInCapitalLettersAtThePaymentTerminal)
            {
                serviceByte = BitHelper.SetBit(serviceByte, 1);
            }

            return serviceByte;
        }
    }
}

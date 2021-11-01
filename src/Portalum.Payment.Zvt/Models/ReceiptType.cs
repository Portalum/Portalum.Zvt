namespace Portalum.Payment.Zvt.Models
{
    /// <summary>
    /// Receipt Type
    /// </summary>
    public enum ReceiptType
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 0x00,
        /// <summary>
        /// Transaction receipt (merchant-receipt)
        /// </summary>
        Merchant = 0x01,
        /// <summary>
        /// Transaction receipt (customer-receipt)
        /// </summary>
        Cardholder = 0x02,
        /// <summary>
        /// Administration receipt
        /// </summary>
        Administration = 0x03
    }
}

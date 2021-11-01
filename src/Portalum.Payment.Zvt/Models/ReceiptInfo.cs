namespace Portalum.Payment.Zvt.Models
{
    /// <summary>
    /// Receipt Info
    /// </summary>
    public class ReceiptInfo
    {
        /// <summary>
        /// Type of the receipt
        /// </summary>
        public ReceiptType ReceiptType { get; set; }
        /// <summary>
        /// Content of the receipt
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// Confirms that all data has been received (End of receipt)
        /// </summary>
        public bool CompletelyProcessed { get; set; }
    }
}

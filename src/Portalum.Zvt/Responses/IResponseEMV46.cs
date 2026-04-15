namespace Portalum.Zvt.Responses
{
    /// <summary>
    /// EMV-print-data (customer-receipt), length variable, ASCII-encoded (not null-terminated) = evaluated
    /// directly printable receipt - DOL for customer-receipt.
    /// </summary>
    /// <remarks>This is required if you want to generate the payment terminal receipt yourself</remarks>
    public interface IResponseEMV46
    {
        /// <summary>
        /// EMV-print-data
        /// </summary>
        string? EMV46 { get; set; }
    }
}

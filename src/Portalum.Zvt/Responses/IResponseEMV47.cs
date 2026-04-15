namespace Portalum.Zvt.Responses
{
    /// <summary>
    /// EMV-print-data (merchant-receipt), length variable, ASCII-encoded (not null-terminated) = evaluated
    /// directly printable receipt - DOL for merchant-receipt.
    /// </summary>
    /// <remarks>This is required if you want to generate the payment terminal receipt yourself</remarks>
    public interface IResponseEMV47
    {
        /// <summary>
        /// EMV-print-data
        /// </summary>
        string? EMV47 { get; set; }
    }
}
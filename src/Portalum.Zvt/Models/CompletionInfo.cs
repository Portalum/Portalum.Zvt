namespace Portalum.Zvt.Models
{
    public class CompletionInfo
    {
        /// <summary>
        /// Status of the issue goods operation
        /// </summary>
        public CompletionInfoState State { get; set; } = CompletionInfoState.Wait;

        /// <summary>
        /// The amount to finally charge the customer. If only a partial issue of goods was possible, a lower than
        /// initially authorized amount can be used.
        /// </summary>
        public decimal Amount { get; set; }
    }
}

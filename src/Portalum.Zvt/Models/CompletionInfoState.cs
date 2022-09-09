namespace Portalum.Zvt.Models
{
    /// <summary>
    /// Status of the issue goods operation
    /// </summary>
    public enum CompletionInfoState
    {
        /// <summary>
        /// This status keeps the issue goods operation in progress and delays the timeout at the payment terminal
        /// </summary>
        Wait,

        /// <summary>
        /// The amount specified in the CompletionInfoStatus is used to change the final amount of the payment 
        /// </summary>
        ChangeAmount,

        /// <summary>
        /// The operation was successful and the amount initially authorized will be charged
        /// </summary>
        Successful,

        /// <summary>
        /// The operation failed and an auto reversal is triggered
        /// </summary>
        Failure
    }
}
namespace Portalum.Zvt.Models
{
    /// <summary>
    /// SendCommand Result
    /// </summary>
    public enum SendCommandResult
    {
        /// <summary>
        /// Acknowledge received
        /// </summary>
        PositiveCompletionReceived,

        /// <summary>
        /// Negative completion received
        /// </summary>
        NegativeCompletionReceived,

        /// <summary>
        /// No data received
        /// </summary>
        NoDataReceived,

        /// <summary>
        /// Send failure
        /// </summary>
        SendFailure,

        /// <summary>
        /// Unknown failure
        /// </summary>
        UnknownFailure
    }
}

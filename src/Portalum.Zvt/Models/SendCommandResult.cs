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
        /// Not supported (other commands, not supported by the PT)
        /// </summary>
        NotSupported,

        /// <summary>
        /// Unknown failure
        /// </summary>
        UnknownFailure
    }
}
